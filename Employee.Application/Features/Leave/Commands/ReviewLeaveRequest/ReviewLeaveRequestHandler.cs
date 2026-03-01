using Employee.Application.Common.Models;
using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Events;
using MediatR;

namespace Employee.Application.Features.Leave.Commands.ReviewLeaveRequest
{
  public class ReviewLeaveRequestHandler : IRequestHandler<ReviewLeaveRequestCommand>
  {
    private readonly ILeaveRequestRepository _repo;
    private readonly ILeaveAllocationService _allocationService;
    private readonly ILeaveTypeRepository _leaveTypeRepo;
    private readonly IAuditLogService _auditService;
    private readonly IPublisher _publisher;

    public ReviewLeaveRequestHandler(
        ILeaveRequestRepository repo,
        ILeaveAllocationService allocationService,
        ILeaveTypeRepository leaveTypeRepo,
        IAuditLogService auditService,
        IPublisher publisher)
    {
      _repo = repo;
      _allocationService = allocationService;
      _leaveTypeRepo = leaveTypeRepo;
      _auditService = auditService;
      _publisher = publisher;
    }

    public async Task Handle(ReviewLeaveRequestCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (entity == null) throw new NotFoundException($"Không tìm thấy đơn nghỉ phép có ID '{request.Id}'");

      var oldStatus = entity.Status;

      // Parse Enum
      if (!Enum.TryParse<Employee.Domain.Enums.LeaveStatus>(request.ReviewDto.Status, true, out var newStatus))
      {
        throw new ValidationException($"Trạng thái '{request.ReviewDto.Status}' không hợp lệ.");
      }

      // Pre-resolve LeaveType BEFORE persisting — fail fast to avoid inconsistent state
      string? leaveTypeDocId = null;
      double workingDays = 0;
      if (newStatus == Employee.Domain.Enums.LeaveStatus.Approved)
      {
        var leaveTypeDoc = await _leaveTypeRepo.GetByCodeAsync(entity.LeaveType.ToString(), cancellationToken);
        if (leaveTypeDoc == null)
          throw new NotFoundException($"Không tìm thấy loại phép '{entity.LeaveType}' trong hệ thống.");
        leaveTypeDocId = leaveTypeDoc.Id;
        // Áp dụng Sandwich Rule nhất quán với CreateLeaveRequestHandler:
        // Nếu loại phép có Sandwich Rule thì tính theo ngày lịch, ngược lại tính ngày làm việc
        workingDays = leaveTypeDoc.IsSandwichRuleApplied
            ? Employee.Application.Common.Utils.DateHelper.CountCalendarDays(entity.FromDate, entity.ToDate)
            : Employee.Application.Common.Utils.DateHelper.CountWorkingDays(entity.FromDate, entity.ToDate);
      }

      // Domain Logic
      if (newStatus == Employee.Domain.Enums.LeaveStatus.Approved)
      {
        entity.Approve(request.ApprovedBy, request.ReviewDto.ManagerComment);
      }
      else if (newStatus == Employee.Domain.Enums.LeaveStatus.Rejected)
      {
        entity.Reject(request.ApprovedBy, request.ReviewDto.ManagerComment ?? "Rejected");
      }
      else
      {
        throw new ValidationException("Chỉ được phép 'Approved' hoặc 'Rejected'.");
      }

      // Optimistic concurrency: rejects with ConcurrencyException (→ HTTP 409) if
      // another request already modified this document since the client last read it.
      await _repo.UpdateAsync(request.Id, entity, request.ExpectedVersion, cancellationToken);

      // Deduct Balance if Approved (leaveType already resolved above)
      if (entity.Status == Employee.Domain.Enums.LeaveStatus.Approved && leaveTypeDocId != null)
      {
        var year = entity.FromDate.Year.ToString();
        await _allocationService.UpdateUsedDaysAsync(entity.EmployeeId, leaveTypeDocId, year, workingDays);
      }

      // Log
      await _auditService.LogAsync(
          userId: request.ApprovedBy,
          userName: request.ApprovedByName,
          action: "REVIEW_LEAVE_REQUEST",
          tableName: "LeaveRequests",
          recordId: request.Id,
          oldVal: new { Status = oldStatus.ToString() },
          newVal: new { Status = entity.Status.ToString(), entity.ManagerComment }
      );

      // Publish Domain Event — decoupled side-effects (notifications, monitoring)
      if (entity.Status == Employee.Domain.Enums.LeaveStatus.Approved)
      {
        await _publisher.Publish(new LeaveRequestApprovedEvent(
            LeaveRequestId: request.Id,
            EmployeeId: entity.EmployeeId,
            ApprovedBy: request.ApprovedBy,
            ManagerComment: entity.ManagerComment,
            WorkingDaysDeducted: workingDays
        ), cancellationToken);
      }
      else if (entity.Status == Employee.Domain.Enums.LeaveStatus.Rejected)
      {
        await _publisher.Publish(
            new DomainEventNotification<LeaveRequestRejectedEvent>(
                new LeaveRequestRejectedEvent(
                    LeaveRequestId: request.Id,
                    EmployeeId: entity.EmployeeId,
                    RejectedBy: request.ApprovedBy,
                    ManagerComment: entity.ManagerComment ?? "Rejected")),
            cancellationToken);
      }
    }
  }
}

