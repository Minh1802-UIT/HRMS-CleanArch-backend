using Employee.Application.Common.Models;
using MediatR;
using Employee.Domain.Events;
using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Leave.EventHandlers
{
  /// <summary>
  /// Handles LeaveRequestRejectedEvent:
  /// - Logs structured info for monitoring dashboards
  /// - NEW-9: Sends in-app notification to the employee with rejection reason
  /// </summary>
  public class LeaveRequestRejectedEventHandler : INotificationHandler<DomainEventNotification<LeaveRequestRejectedEvent>>
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

    public async Task Handle(DomainEventNotification<LeaveRequestRejectedEvent> notificationWrapper, CancellationToken cancellationToken)
    {
      var evt = notificationWrapper.DomainEvent;
      _logger.LogInformation(
          "[LeaveEvent] Rejected — LeaveRequestId: {LeaveRequestId}, EmployeeId: {EmployeeId}, RejectedBy: {RejectedBy}, Comment: {Comment}",
          evt.LeaveRequestId, evt.EmployeeId, evt.RejectedBy, evt.ManagerComment);

      // NEW-9: Create in-app notification for the employee
      var body = string.IsNullOrWhiteSpace(evt.ManagerComment)
          ? "Your leave request has been rejected."
          : $"Your leave request has been rejected. Reason: {evt.ManagerComment}";

      await _notificationService.CreateAsync(
          userId: evt.EmployeeId,
          title: "Leave Request Rejected ❌",
          body: body,
          type: "LeaveRejected",
          referenceId: notificationWrapper.DomainEvent.LeaveRequestId,
          referenceType: "LeaveRequest",
          cancellationToken: cancellationToken);
    }
  }
}

