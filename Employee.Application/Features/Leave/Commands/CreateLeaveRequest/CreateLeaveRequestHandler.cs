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
      Employee.Domain.Enums.LeaveTypeEnum leaveTypeEnum;
      Employee.Domain.Entities.Leave.LeaveType? resolvedType;

      // Try parsing as enum first (e.g., "Annual", "Sick", "Unpaid")
      if (Enum.TryParse<Employee.Domain.Enums.LeaveTypeEnum>(request.LeaveType, true, out leaveTypeEnum))
      {
        // It's an enum name — resolve the document ID via Code
        resolvedType = await _leaveTypeRepo.GetByCodeAsync(request.LeaveType, cancellationToken);
        if (resolvedType == null)
          throw new NotFoundException($"Không tìm thấy loại phép '{request.LeaveType}' trong hệ thống.");
        leaveTypeId = resolvedType.Id;
      }
      else
      {
        // Assume it's a document ID — look up by ID to get the Code for enum parsing
        resolvedType = await _leaveTypeRepo.GetByIdAsync(request.LeaveType, cancellationToken);
        if (resolvedType == null)
          throw new NotFoundException($"Không tìm thấy loại phép có ID '{request.LeaveType}'.");

        if (!Enum.TryParse<Employee.Domain.Enums.LeaveTypeEnum>(resolvedType.Code, true, out leaveTypeEnum))
          throw new ValidationException($"Mã loại phép '{resolvedType.Code}' không hợp lệ.");
        leaveTypeId = resolvedType.Id;
      }

      // 1.1 Check Overlap (Chống trùng lịch)
      var hasOverlap = await _repo.ExistsOverlapAsync(request.EmployeeId, request.FromDate, request.ToDate, cancellationToken: cancellationToken);
      if (hasOverlap)
      {
        throw new ConflictException("Khoảng thời gian này bạn đã có đơn nghỉ phép khác (đang chờ duyệt hoặc đã duyệt).");
      }

      // 2. Check số dư (Balance Validation) — use resolved document ID
      // NEW-4 Sandwich Rule: if the leave type applies the sandwich rule, count ALL calendar days
      // (weekends between leave days are consumed). Otherwise count only working days.
      var daysRequested = resolvedType.IsSandwichRuleApplied
          ? Employee.Application.Common.Utils.DateHelper.CountCalendarDays(request.FromDate, request.ToDate)
          : Employee.Application.Common.Utils.DateHelper.CountWorkingDays(request.FromDate, request.ToDate);

      var year = request.FromDate.Year.ToString();
      var balance = await _allocationService.GetByEmployeeAndTypeAsync(request.EmployeeId, leaveTypeId, year);
      var currentBalance = balance?.RemainingDays ?? 0;

      if (currentBalance < daysRequested)
      {
        throw new ValidationException($"Số dư phép không đủ. Hiện có {currentBalance}, yêu cầu {daysRequested} (chỉ tính ngày làm việc).");
      }

      // 3. Create Entity directly (DDD)
      var entity = new Employee.Domain.Entities.Leave.LeaveRequest(
          request.EmployeeId,
          leaveTypeEnum,
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

