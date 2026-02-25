using Employee.Application.Common.Interfaces.Common;
using Employee.Domain.Entities.Notifications;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface INotificationRepository : IBaseRepository<Notification>
  {
    Task<List<Notification>> GetByUserIdAsync(string userId, bool unreadOnly = false, int limit = 50, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default);
    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);
  }
}
