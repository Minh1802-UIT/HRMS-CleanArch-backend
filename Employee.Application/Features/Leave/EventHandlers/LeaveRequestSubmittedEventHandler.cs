using MediatR;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Leave.Events;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Leave.EventHandlers
{
  /// <summary>
  /// Handles LeaveRequestSubmittedEvent:
  /// - Logs audit trail for the submission
  /// - Extension point: send notification to the manager
  /// </summary>
  public class LeaveRequestSubmittedEventHandler : INotificationHandler<LeaveRequestSubmittedEvent>
  {
    private readonly IAuditLogService _auditService;
    private readonly ILogger<LeaveRequestSubmittedEventHandler> _logger;

    public LeaveRequestSubmittedEventHandler(
        IAuditLogService auditService,
        ILogger<LeaveRequestSubmittedEventHandler> logger)
    {
      _auditService = auditService;
      _logger = logger;
    }

    public async Task Handle(LeaveRequestSubmittedEvent notification, CancellationToken cancellationToken)
    {
      _logger.LogInformation(
          "[LeaveEvent] Submitted — EmployeeId: {EmployeeId}, LeaveRequestId: {LeaveRequestId}, Type: {LeaveType}, Days: {Days}",
          notification.EmployeeId, notification.LeaveRequestId, notification.LeaveType, notification.Days);

      await _auditService.LogAsync(
          userId: notification.EmployeeId,
          userName: notification.EmployeeId,
          action: "SUBMIT_LEAVE_REQUEST",
          tableName: "LeaveRequests",
          recordId: notification.LeaveRequestId,
          oldVal: null,
          newVal: new
          {
            notification.LeaveType,
            notification.FromDate,
            notification.ToDate,
            notification.Days,
            notification.Reason,
            Status = "Pending"
          }
      );

      // TODO: Notify manager via email/push notification
      // await _notificationService.NotifyManagerAsync(notification.EmployeeId, notification.LeaveRequestId);
    }
  }
}
