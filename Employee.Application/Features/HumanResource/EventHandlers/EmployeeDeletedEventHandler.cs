using Employee.Application.Common.Models;
using MediatR;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Events;
using Employee.Application.Features.Auth.Commands.DeleteUser;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<EmployeeDeletedEventHandler> _logger;

    public EmployeeDeletedEventHandler(
        ISender sender,
        IAuditLogService auditService,
        ICurrentUser currentUser,
        IContractRepository contractRepo,
        IAttendanceRepository attendanceRepo,
        IRawAttendanceLogRepository rawAttendanceRepo,
        ILeaveRequestRepository leaveRepo,
        ILeaveAllocationRepository allocationRepo,
        IPayrollRepository payrollRepo,
        ILogger<EmployeeDeletedEventHandler> logger)
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
      _logger = logger;
    }

    public async Task Handle(DomainEventNotification<EmployeeDeletedEvent> notificationWrapper, CancellationToken cancellationToken)
    {
      // 1. Xóa tài khoản User (Decoupled)
      await _sender.Send(new DeleteUserByEmployeeIdCommand { EmployeeId = notificationWrapper.DomainEvent.EmployeeId }, cancellationToken);

      // 2. Cleanup all related data — each step is isolated so a single failure
      //    does not leave the rest of the data uncleaned.
      var errors = new List<Exception>();

      async Task TryDelete(Func<Task> step, string stepName)
      {
        try { await step(); }
        catch (Exception ex)
        {
          errors.Add(ex);
          _logger.LogWarning(ex,
            "Failed to cleanup {StepName} for deleted employee {EmployeeId}. " +
            "Manual cleanup may be required.",
            stepName, notificationWrapper.DomainEvent.EmployeeId);
        }
      }

      await TryDelete(() => _contractRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken), "Contracts");
      await TryDelete(() => _attendanceRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken), "AttendanceBuckets");
      await TryDelete(() => _rawAttendanceRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken), "RawAttendanceLogs");
      await TryDelete(() => _leaveRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken), "LeaveRequests");
      await TryDelete(() => _allocationRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken), "LeaveAllocations");
      await TryDelete(() => _payrollRepo.DeleteByEmployeeIdAsync(notificationWrapper.DomainEvent.EmployeeId, cancellationToken), "Payrolls");

      if (errors.Count > 0)
      {
        _logger.LogError(
          "Employee deletion cleanup completed with {ErrorCount} error(s) for employee {EmployeeId}. " +
          "Related data may remain orphaned. Errors: {Errors}",
          errors.Count, notificationWrapper.DomainEvent.EmployeeId,
          string.Join("; ", errors.Select(e => e.Message)));
      }

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

