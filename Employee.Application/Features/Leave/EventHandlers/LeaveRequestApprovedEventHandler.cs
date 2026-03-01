using Employee.Application.Common.Models;
using MediatR;
using Employee.Domain.Events;
using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Leave.EventHandlers
{
  /// <summary>
  /// Handles LeaveRequestApprovedEvent:
  /// - Logs structured info for monitoring dashboards
  /// - NEW-9: Sends in-app notification to the employee
  /// </summary>
  public class LeaveRequestApprovedEventHandler : INotificationHandler<DomainEventNotification<LeaveRequestApprovedEvent>>
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

    public async Task Handle(DomainEventNotification<LeaveRequestApprovedEvent> notificationWrapper, CancellationToken cancellationToken)
    {
      _logger.LogInformation(
          "[LeaveEvent] Approved — LeaveRequestId: {LeaveRequestId}, EmployeeId: {EmployeeId}, ApprovedBy: {ApprovedBy}, Days: {Days}",
          notificationWrapper.DomainEvent.LeaveRequestId, notificationWrapper.DomainEvent.EmployeeId,
          notificationWrapper.DomainEvent.ApprovedBy, notificationWrapper.DomainEvent.WorkingDaysDeducted);

      // NEW-9: Create in-app notification for the employee
      await _notificationService.CreateAsync(
          userId: notificationWrapper.DomainEvent.EmployeeId,
          title: "Leave Request Approved ✅",
          body: $"Your leave request ({notificationWrapper.DomainEvent.WorkingDaysDeducted} day(s)) has been approved by {notificationWrapper.DomainEvent.ApprovedBy}.",
          type: "LeaveApproved",
          referenceId: notificationWrapper.DomainEvent.LeaveRequestId,
          referenceType: "LeaveRequest",
          cancellationToken: cancellationToken);
    }
  }
}

