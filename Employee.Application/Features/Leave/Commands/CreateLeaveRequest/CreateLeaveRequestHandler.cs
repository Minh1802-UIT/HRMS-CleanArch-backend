using Employee.Application.Common.Models;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Events;
using Employee.Application.Features.Leave.Mappers;
using MediatR;
using Employee.Application.Common.Utils; // For DateHelper

namespace Employee.Application.Features.Leave.Commands.CreateLeaveRequest
{
  public class CreateLeaveRequestHandler : IRequestHandler<CreateLeaveRequestCommand, LeaveRequestDto>
  {
    private readonly ILeaveRequestRepository _repo;
    private readonly IEmployeeRepository _empRepo;
    private readonly ILeaveAllocationService _allocationService;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IPublisher _publisher;

    public CreateLeaveRequestHandler(
        ILeaveRequestRepository repo,
        IEmployeeRepository empRepo,
        ILeaveAllocationService allocationService,
        ILeaveTypeRepository leaveTypeRepo,
        IPublisher publisher)
    {
      _repo = repo;
      _empRepo = empRepo;
      _allocationService = allocationService;
      _leaveTypeRepo = leaveTypeRepo;
      _publisher = publisher;
    }

    public async Task<LeaveRequestDto> Handle(CreateLeaveRequestCommand request, CancellationToken cancellationToken)
    {
      // 1.0 Resolve LeaveType: request.LeaveType can be either a document ID or an enum code
      string leaveTypeId;
      Employee.Domain.Enums.LeaveCategory leaveCategory;
      Employee.Domain.Entities.Leave.LeaveType? resolvedType;

      // Try parsing as enum first (e.g., "Annual", "Sick", "Unpaid")
      if (Enum.TryParse<Employee.Domain.Enums.LeaveCategory>(request.LeaveType, true, out leaveCategory))
      {
        // It's an enum name — resolve the document ID via Code
        resolvedType = await _leaveTypeRepo.GetByCodeAsync(request.LeaveType, cancellationToken);
        if (resolvedType == null)
          throw new NotFoundException($"Leave type '{request.LeaveType}' not found in the system.");
        leaveTypeId = resolvedType.Id;
      }
      else
      {
        // Assume it's a document ID — look up by ID to get the Code for enum parsing
        resolvedType = await _leaveTypeRepo.GetByIdAsync(request.LeaveType, cancellationToken);
        if (resolvedType == null)
          throw new NotFoundException($"Leave type with ID '{request.LeaveType}' not found.");

        if (!Enum.TryParse<Employee.Domain.Enums.LeaveCategory>(resolvedType.Code, true, out leaveCategory))
          throw new ValidationException($"Leave type code '{resolvedType.Code}' is not a valid leave category.");
        leaveTypeId = resolvedType.Id;
      }

      // 1.1 Check for scheduling overlap
      var hasOverlap = await _repo.ExistsOverlapAsync(request.EmployeeId, request.FromDate, request.ToDate, cancellationToken: cancellationToken);
      if (hasOverlap)
      {
        throw new ConflictException("A leave request already exists for this date range (pending or approved).");
      }

      // 2. Balance validation — use resolved document ID
      // Sandwich Rule: if the leave type applies the sandwich rule, count ALL calendar days
      // (weekends between leave days are consumed). Otherwise count only working days.
      var daysRequested = resolvedType.IsSandwichRuleApplied
          ? Employee.Application.Common.Utils.DateHelper.CountCalendarDays(request.FromDate, request.ToDate)
          : Employee.Application.Common.Utils.DateHelper.CountWorkingDays(request.FromDate, request.ToDate);

      var year = request.FromDate.Year.ToString();
      var balance = await _allocationService.GetByEmployeeAndTypeAsync(request.EmployeeId, leaveTypeId, year);
      var currentBalance = balance?.RemainingDays ?? 0;

      if (currentBalance < daysRequested)
      {
        throw new ValidationException($"Insufficient leave balance. Available: {currentBalance} day(s), requested: {daysRequested} day(s).");
      }

      // 3. Create Entity directly (DDD)
      var entity = new Employee.Domain.Entities.Leave.LeaveRequest(
          request.EmployeeId,
          leaveCategory,
          request.FromDate,
          request.ToDate,
          request.Reason
      );

      // 4. Save
      await _repo.CreateAsync(entity, cancellationToken);
      // NOTE: Balance deduction is intentionally NOT done here.
      // Deduction only happens in ReviewLeaveRequestHandler when status = Approved,
      // to avoid double-deducting (once on submit + once on approve).
      // The balance check above (step 2) is the submission guard.

      // 5. Return DTO
      var emp = await _empRepo.GetByIdAsync(request.EmployeeId, cancellationToken);
      var name = emp?.FullName ?? "Unknown";
      var code = emp?.EmployeeCode ?? "Unknown";

      var dto = entity.ToDto(name, code);

      // 6. Publish Domain Event — decoupled side-effects (audit, notifications)
      await _publisher.Publish(
          new DomainEventNotification<LeaveRequestSubmittedEvent>(
              new LeaveRequestSubmittedEvent(
                  LeaveRequestId: entity.Id,
                  EmployeeId: entity.EmployeeId,
                  LeaveType: entity.LeaveType.ToString(),
                  FromDate: entity.FromDate,
                  ToDate: entity.ToDate,
                  Reason: entity.Reason)),
          cancellationToken);

      return dto;
    }
  }
}

