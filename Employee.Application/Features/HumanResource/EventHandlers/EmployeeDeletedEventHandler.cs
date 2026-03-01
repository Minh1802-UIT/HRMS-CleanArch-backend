using Employee.Application.Common.Models;
using MediatR;
using Employee.Application.Common.Interfaces; // ICurrentUser
using Employee.Application.Common.Interfaces.Organization.IService; // IAuditLogService
using Employee.Domain.Interfaces.Repositories; // IMP-3
using Employee.Domain.Events;
using Employee.Application.Features.Auth.Commands.DeleteUser;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
  public class EmployeeDeletedEventHandler : INotificationHandler<DomainEventNotification<EmployeeDeletedEvent>>
  {
    private readonly ISender _sender;
    private readonly IAuditLogService _auditService;
    private readonly ICurrentUser _currentUser;
    private readonly IContractRepository _contractRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IRawAttendanceLogRepository _rawAttendanceRepo;
    private readonly ILeaveRequestRepository _leaveRepo;
    private readonly ILeaveAllocationRepository _allocationRepo;
    private readonly IPayrollRepository _payrollRepo;

    public EmployeeDeletedEventHandler(
        ISender sender,
        IAuditLogService auditService,
        ICurrentUser currentUser,
        IContractRepository contractRepo,
        IAttendanceRepository attendanceRepo,
        IRawAttendanceLogRepository rawAttendanceRepo,
        ILeaveRequestRepository leaveRepo,
        ILeaveAllocationRepository allocationRepo,
        IPayrollRepository payrollRepo)
    {
      _sender = sender;
      _auditService = auditService;
      _currentUser = currentUser;
      _contractRepo = contractRepo;
      _attendanceRepo = attendanceRepo;
      _rawAttendanceRepo = rawAttendanceRepo;
      _leaveRepo = leaveRepo;
      _allocationRepo = allocationRepo;
      _payrollRepo = payrollRepo;
    }

    public async Task Handle(DomainEventNotification<EmployeeDeletedEvent> notificationWrapper, CancellationToken cancellationToken)
    {
      // 1. Xóa tài khoản User (Decoupled)
      await _sender.Send(new DeleteUserByEmployeeIdCommand { EmployeeId = notificationWrapper.DomainEvent.EmployeeId }, cancellationToken);

      // 2. Cleanup all related data — each step is isolated so a single failure
      //    does not leave the rest of the data uncleaned.
      var errors = new List<Exception>();

      async Task TryDelete(Func<Task> step)
      {
        try { await step(); }
        catch (Exception ex) { errors.Add(ex); }
      }

      await TryDelete(() => _contractRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken));
      await TryDelete(() => _attendanceRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken));
      await TryDelete(() => _rawAttendanceRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken));
      await TryDelete(() => _leaveRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken));
      await TryDelete(() => _allocationRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken));
      await TryDelete(() => _payrollRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken));

      // 3. Ghi Log (Decoupled)
      await _auditService.LogAsync(
          userId: _currentUser.UserId ?? "System",
          userName: _currentUser.UserName ?? "System",
          action: "DELETE_EMPLOYEE",
          tableName: "Employees",
          recordId: notificationWrapper.DomainEvent.EmployeeId,
          oldVal: new { Name = notificationWrapper.DomainEvent.FullName, Code = notificationWrapper.DomainEvent.EmployeeCode },
          newVal: null
      );
    }
  }
}

