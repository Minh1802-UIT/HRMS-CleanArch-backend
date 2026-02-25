using MediatR;
using Employee.Application.Features.Leave.Events;
using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Leave.EventHandlers
{
  /// <summary>
  /// Handles LeaveRequestRejectedEvent:
  /// - Logs structured info for monitoring dashboards
  /// - NEW-9: Sends in-app notification to the employee with rejection reason
  /// </summary>
  public class LeaveRequestRejectedEventHandler : INotificationHandler<LeaveRequestRejectedEvent>
  {
    private readonly ILogger<LeaveRequestRejectedEventHandler> _logger;
    private readonly INotificationService _notificationService;

    public LeaveRequestRejectedEventHandler(
        ILogger<LeaveRequestRejectedEventHandler> logger,
        INotificationService notificationService)
    {
      _logger = logger;
      _notificationService = notificationService;
    }

    public async Task Handle(LeaveRequestRejectedEvent notification, CancellationToken cancellationToken)
    {
      _logger.LogInformation(
          "[LeaveEvent] Rejected — LeaveRequestId: {LeaveRequestId}, EmployeeId: {EmployeeId}, RejectedBy: {RejectedBy}, Reason: {Reason}",
          notification.LeaveRequestId, notification.EmployeeId,
          notification.RejectedBy, notification.RejectionReason);

      // NEW-9: Create in-app notification for the employee
      var body = string.IsNullOrWhiteSpace(notification.RejectionReason)
          ? "Your leave request has been rejected."
          : $"Your leave request has been rejected. Reason: {notification.RejectionReason}";

      await _notificationService.CreateAsync(
          userId: notification.EmployeeId,
          title: "Leave Request Rejected ❌",
          body: body,
          type: "LeaveRejected",
          referenceId: notification.LeaveRequestId,
          referenceType: "LeaveRequest",
          cancellationToken: cancellationToken);
    }
  }
}
