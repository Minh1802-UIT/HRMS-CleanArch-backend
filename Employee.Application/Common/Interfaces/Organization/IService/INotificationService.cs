using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface INotificationService
  {
    /// <summary>Create and persist an in-app notification for a user.</summary>
    Task CreateAsync(string userId, string title, string body, string type, string? referenceId = null, string? referenceType = null, CancellationToken cancellationToken = default);

    /// <summary>Get recent notifications for a user (max 50, newest first).</summary>
    Task<IEnumerable<NotificationDto>> GetByUserIdAsync(string userId, bool unreadOnly = false, CancellationToken cancellationToken = default);

    /// <summary>Count unread notifications for a user.</summary>
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Mark a single notification as read (must belong to the user).</summary>
    Task<bool> MarkReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default);

    /// <summary>Mark all of a user's notifications as read.</summary>
    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
  }

  /// <summary>DTO projected from Notification entity.</summary>
  public class NotificationDto
  {
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public System.DateTime CreatedAt { get; set; }
  }
}
