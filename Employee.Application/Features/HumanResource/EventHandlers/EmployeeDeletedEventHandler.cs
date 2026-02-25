using MediatR;
using Employee.Application.Common.Interfaces; // ICurrentUser
using Employee.Application.Common.Interfaces.Organization.IService; // IAuditLogService
using Employee.Application.Common.Interfaces.Organization.IRepository; // IMP-3
using Employee.Application.Features.HumanResource.Events;
using Employee.Application.Features.Auth.Commands.DeleteUser;

namespace Employee.Application.Features.HumanResource.EventHandlers
{
  public class EmployeeDeletedEventHandler : INotificationHandler<EmployeeDeletedEvent>
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

    public async Task Handle(EmployeeDeletedEvent notification, CancellationToken cancellationToken)
    {
      // 1. Xóa tài khoản User (Decoupled)
      await _sender.Send(new DeleteUserByEmployeeIdCommand { EmployeeId = notification.EmployeeId }, cancellationToken);

      // 2. Cleanup all related data — each step is isolated so a single failure
      //    does not leave the rest of the data uncleaned.
      var errors = new List<Exception>();

      async Task TryDelete(Func<Task> step)
      {
        try { await step(); }
        catch (Exception ex) { errors.Add(ex); }
      }

      await TryDelete(() => _contractRepo.DeleteByEmployeeIdAsync(notification.EmployeeId, cancellationToken));
      await TryDelete(() => _attendanceRepo.DeleteByEmployeeIdAsync(notification.EmployeeId, cancellationToken));
      await TryDelete(() => _rawAttendanceRepo.DeleteByEmployeeIdAsync(notification.EmployeeId, cancellationToken));
      await TryDelete(() => _leaveRepo.DeleteByEmployeeIdAsync(notification.EmployeeId, cancellationToken));
      await TryDelete(() => _allocationRepo.DeleteByEmployeeIdAsync(notification.EmployeeId, cancellationToken));
      await TryDelete(() => _payrollRepo.DeleteByEmployeeIdAsync(notification.EmployeeId, cancellationToken));

      // 3. Ghi Log (Decoupled)
      await _auditService.LogAsync(
          userId: _currentUser.UserId ?? "System",
          userName: _currentUser.UserName ?? "System",
          action: "DELETE_EMPLOYEE",
          tableName: "Employees",
          recordId: notification.EmployeeId,
          oldVal: new { Name = notification.FullName, Code = notification.EmployeeCode },
          newVal: null
      );
    }
  }
}
