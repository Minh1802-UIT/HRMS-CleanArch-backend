using MediatR;
using Employee.Application.Features.Leave.Events;
using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Leave.EventHandlers
{
  /// <summary>
  /// Handles LeaveRequestApprovedEvent:
  /// - Logs structured info for monitoring dashboards
  /// - NEW-9: Sends in-app notification to the employee
  /// </summary>
  public class LeaveRequestApprovedEventHandler : INotificationHandler<LeaveRequestApprovedEvent>
  {
    private readonly ILogger<LeaveRequestApprovedEventHandler> _logger;
    private readonly INotificationService _notificationService;

    public LeaveRequestApprovedEventHandler(
        ILogger<LeaveRequestApprovedEventHandler> logger,
        INotificationService notificationService)
    {
      _logger = logger;
      _notificationService = notificationService;
    }

    public async Task Handle(LeaveRequestApprovedEvent notification, CancellationToken cancellationToken)
    {
      _logger.LogInformation(
          "[LeaveEvent] Approved — LeaveRequestId: {LeaveRequestId}, EmployeeId: {EmployeeId}, ApprovedBy: {ApprovedBy}, Days: {Days}",
          notification.LeaveRequestId, notification.EmployeeId,
          notification.ApprovedBy, notification.WorkingDaysDeducted);

      // NEW-9: Create in-app notification for the employee
      await _notificationService.CreateAsync(
          userId: notification.EmployeeId,
          title: "Leave Request Approved ✅",
          body: $"Your leave request ({notification.WorkingDaysDeducted} day(s)) has been approved by {notification.ApprovedBy}.",
          type: "LeaveApproved",
          referenceId: notification.LeaveRequestId,
          referenceType: "LeaveRequest",
          cancellationToken: cancellationToken);
    }
  }
}
