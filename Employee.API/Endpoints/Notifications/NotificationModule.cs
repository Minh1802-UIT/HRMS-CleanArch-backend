using Carter;

namespace Employee.API.Endpoints.Notifications
{
  public class NotificationModule : ICarterModule
  {
    public void AddRoutes(IEndpointRouteBuilder app)
    {
      var group = app.MapGroup("/api/notifications")
                     .WithTags("Notifications")
                     .RequireAuthorization();

      // Get my notifications (optional ?unreadOnly=true query param)
      group.MapGet("/", NotificationHandlers.GetMyNotifications);

      // Get unread count (used by navbar bell badge)
      group.MapGet("/unread-count", NotificationHandlers.GetUnreadCount);

      // Mark a single notification as read
      group.MapPut("/{id}/read", NotificationHandlers.MarkRead);

      // Mark all notifications as read
      group.MapPost("/read-all", NotificationHandlers.MarkAllRead);
    }
  }
}
