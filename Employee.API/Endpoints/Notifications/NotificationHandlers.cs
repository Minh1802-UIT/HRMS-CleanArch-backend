using Employee.API.Common;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Notifications
{
  public static class NotificationHandlers
  {
    // GET /api/notifications — get my notifications
    public static async Task<IResult> GetMyNotifications(
        [FromServices] ICurrentUser currentUser,
        [FromServices] INotificationService service,
        [FromQuery] bool unreadOnly = false)
    {
      // Notifications are keyed by EmployeeId (set by event handlers). Fall back to UserId for admin/non-employee users.
      var targetId = currentUser.EmployeeId ?? currentUser.UserId;
      var list = await service.GetByUserIdAsync(targetId, unreadOnly);
      return ResultUtils.Success(list, "Notifications retrieved successfully.");
    }

    // GET /api/notifications/unread-count
    public static async Task<IResult> GetUnreadCount(
        [FromServices] ICurrentUser currentUser,
        [FromServices] INotificationService service)
    {
      var targetId = currentUser.EmployeeId ?? currentUser.UserId;
      var count = await service.GetUnreadCountAsync(targetId);
      return ResultUtils.Success(count, "Unread notification count.");
    }

    // PUT /api/notifications/{id}/read — mark single notification as read
    public static async Task<IResult> MarkRead(
        string id,
        [FromServices] ICurrentUser currentUser,
        [FromServices] INotificationService service)
    {
      // Notifications are keyed by EmployeeId; fall back to UserId for admin/non-employee users
      var targetId = currentUser.EmployeeId ?? currentUser.UserId;
      var success = await service.MarkReadAsync(id, targetId);
      if (!success)
        return ResultUtils.Fail("NOT_FOUND", "Notification not found or does not belong to you.");

      return ResultUtils.Success("Notification marked as read.");
    }

    // POST /api/notifications/read-all — mark all as read
    public static async Task<IResult> MarkAllRead(
        [FromServices] ICurrentUser currentUser,
        [FromServices] INotificationService service)
    {
      var targetId = currentUser.EmployeeId ?? currentUser.UserId;
      await service.MarkAllReadAsync(targetId);
      return ResultUtils.Success("All notifications marked as read.");
    }
  }
}
