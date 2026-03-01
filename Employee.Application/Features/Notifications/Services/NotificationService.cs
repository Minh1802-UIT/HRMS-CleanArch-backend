using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Notifications.Mappers;
using Employee.Domain.Entities.Notifications;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Notifications.Services
{
  public class NotificationService : INotificationService
  {
    private readonly INotificationRepository _repo;
    private readonly Employee.Domain.Interfaces.Common.IDateTimeProvider _dateTime;

    public NotificationService(INotificationRepository repo, Employee.Domain.Interfaces.Common.IDateTimeProvider dateTime)
    {
      _repo = repo;
      _dateTime = dateTime;
    }

    public async Task CreateAsync(
        string userId,
        string title,
        string body,
        string type,
        string? referenceId = null,
        string? referenceType = null,
        CancellationToken cancellationToken = default)
    {
      var notification = new Notification(userId, title, body, type, referenceId, referenceType);
      await _repo.CreateAsync(notification, cancellationToken);
    }

    public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(
        string userId,
        bool unreadOnly = false,
        CancellationToken cancellationToken = default)
    {
      var list = await _repo.GetByUserIdAsync(userId, unreadOnly, 50, cancellationToken);
      return list.Select(n => n.ToDto());
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
      => await _repo.GetUnreadCountAsync(userId, cancellationToken);

    public async Task<bool> MarkReadAsync(string notificationId, string userId, CancellationToken cancellationToken = default)
    {
      // Only allow the owner to mark as read
      var notification = await _repo.GetByIdAsync(notificationId, cancellationToken);
      if (notification == null || notification.UserId != userId) return false;

      notification.MarkRead(_dateTime.UtcNow);
      await _repo.UpdateAsync(notificationId, notification, cancellationToken);
      return true;
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
      => await _repo.MarkAllReadAsync(userId, cancellationToken);
  }
}

