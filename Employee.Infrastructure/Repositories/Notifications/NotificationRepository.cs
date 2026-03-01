using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Notifications;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Infrastructure.Repositories.Notifications
{
  public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
  {
    public NotificationRepository(IMongoContext context) : base(context, "notifications")
    {
    }

    public async Task<List<Notification>> GetByUserIdAsync(
        string userId,
        bool unreadOnly = false,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
      var filterBuilder = Builders<Notification>.Filter;
      var filter = filterBuilder.And(
          filterBuilder.Eq(n => n.IsDeleted, false),
          filterBuilder.Eq(n => n.UserId, userId)
      );

      if (unreadOnly)
        filter = filterBuilder.And(filter, filterBuilder.Eq(n => n.IsRead, false));

      return await _collection
          .Find(filter)
          .Sort(Builders<Notification>.Sort.Descending(n => n.CreatedAt))
          .Limit(limit)
          .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<Notification>.Filter.And(
          Builders<Notification>.Filter.Eq(n => n.IsDeleted, false),
          Builders<Notification>.Filter.Eq(n => n.UserId, userId),
          Builders<Notification>.Filter.Eq(n => n.IsRead, false)
      );

      var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
      return (int)count;
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<Notification>.Filter.And(
          Builders<Notification>.Filter.Eq(n => n.IsDeleted, false),
          Builders<Notification>.Filter.Eq(n => n.UserId, userId),
          Builders<Notification>.Filter.Eq(n => n.IsRead, false)
      );

      var update = Builders<Notification>.Update
          .Set(n => n.IsRead, true)
          .Set(n => n.UpdatedAt, System.DateTime.UtcNow);

      await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
  }
}
