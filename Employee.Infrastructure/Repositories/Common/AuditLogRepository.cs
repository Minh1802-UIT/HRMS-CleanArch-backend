using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Common;
using MongoDB.Driver;
using Employee.Application.Common.Models;
using Employee.Infrastructure.Persistence;

namespace Employee.Infrastructure.Repositories.Common
{
  public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
  {
    public AuditLogRepository(IMongoContext context) : base(context, "audit_logs")
    {
    }

    public async Task<List<AuditLog>> GetLogsAsync(int limit, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(_ => true)
                       .SortByDescending(x => x.CreatedAt)
                       .Limit(limit)
                       .ToListAsync(cancellationToken);
    }

    public async Task<(List<AuditLog> Logs, long TotalCount)> GetLogsPagedAsync(PaginationParams pagination, DateTime? start, DateTime? end, string? userId, string? actionType, CancellationToken cancellationToken = default)
    {
      var filterBuilder = Builders<AuditLog>.Filter;
      var filter = filterBuilder.Empty;

      if (start.HasValue)
        filter &= filterBuilder.Gte(x => x.CreatedAt, start.Value);

      if (end.HasValue)
        filter &= filterBuilder.Lte(x => x.CreatedAt, end.Value);

      if (!string.IsNullOrEmpty(userId))
        filter &= filterBuilder.Eq(x => x.UserId, userId);

      if (!string.IsNullOrEmpty(actionType))
        filter &= filterBuilder.Eq(x => x.Action, actionType);

      if (!string.IsNullOrEmpty(pagination.SearchTerm))
      {
        var search = pagination.SearchTerm.ToLower();
        filter &= filterBuilder.Or(
            filterBuilder.Regex(x => x.UserName, new MongoDB.Bson.BsonRegularExpression(search, "i")),
            filterBuilder.Regex(x => x.TableName, new MongoDB.Bson.BsonRegularExpression(search, "i")),
            filterBuilder.Regex(x => x.Action, new MongoDB.Bson.BsonRegularExpression(search, "i"))
        );
      }

      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

      var pageNumber = pagination.PageNumber ?? 1;
      var pageSize = pagination.PageSize ?? 20;

      var logs = await _collection.Find(filter)
                           .SortByDescending(x => x.CreatedAt)
                           .Skip((pageNumber - 1) * pageSize)
                           .Limit(pageSize)
                           .ToListAsync(cancellationToken);

      return (logs, totalCount);
    }
  }
}
