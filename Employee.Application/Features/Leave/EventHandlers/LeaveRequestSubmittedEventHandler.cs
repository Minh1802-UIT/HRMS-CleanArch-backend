using Employee.Application.Common.Models;
using MediatR;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Leave.EventHandlers
{
  /// <summary>
  /// Handles LeaveRequestSubmittedEvent:
  /// - Logs audit trail for the submission
  /// - Extension point: send notification to the manager
  /// </summary>
  public class LeaveRequestSubmittedEventHandler : INotificationHandler<DomainEventNotification<LeaveRequestSubmittedEvent>>
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

    public async Task Handle(DomainEventNotification<LeaveRequestSubmittedEvent> notificationWrapper, CancellationToken cancellationToken)
    {
      var evt = notificationWrapper.DomainEvent;
      _logger.LogInformation(
          "[LeaveEvent] Submitted — EmployeeId: {EmployeeId}, LeaveRequestId: {LeaveRequestId}, Type: {LeaveType}",
          evt.EmployeeId, evt.LeaveRequestId, evt.LeaveType);

      await _auditService.LogAsync(
          userId: evt.EmployeeId,
          userName: evt.EmployeeId,
          action: "SUBMIT_LEAVE_REQUEST",
          tableName: "LeaveRequests",
          recordId: evt.LeaveRequestId,
          oldVal: null,
          newVal: new
          {
            evt.LeaveType,
            evt.FromDate,
            evt.ToDate,
            evt.Reason,
            Status = "Pending"
          }
      );

      // TODO: Notify manager via email/push notification
      // await _notificationService.NotifyManagerAsync(notificationWrapper.DomainEvent.EmployeeId, notificationWrapper.DomainEvent.LeaveRequestId);
    }
  }
}

