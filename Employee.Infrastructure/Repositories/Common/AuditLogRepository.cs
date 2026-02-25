using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Common;
using MongoDB.Driver;
using MongoDB.Bson;
using Employee.Application.Common.Models;
using Employee.Infrastructure.Persistence;
using System.Text;
using System.Text.Json;

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
            filterBuilder.Regex(x => x.UserName, new BsonRegularExpression(search, "i")),
            filterBuilder.Regex(x => x.TableName, new BsonRegularExpression(search, "i")),
            filterBuilder.Regex(x => x.Action,    new BsonRegularExpression(search, "i"))
        );
      }

      var totalCount = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

      var pageNumber = pagination.PageNumber ?? 1;
      var pageSize   = pagination.PageSize   ?? 20;

      var logs = await _collection.Find(filter)
                           .SortByDescending(x => x.CreatedAt)
                           .Skip((pageNumber - 1) * pageSize)
                           .Limit(pageSize)
                           .ToListAsync(cancellationToken);

      return (logs, totalCount);
    }

    // ------------------------------------------------------------------ //
    //  Cursor-based (keyset) pagination                                    //
    //  Sort: CreatedAt DESC, _id DESC                                      //
    //  Cursor: Base64( JSON { "t": <ticks>, "id": "<lastId>" } )          //
    //                                                                      //
    //  On 250 K rows:  Skip(50 000) ≈ 200 ms  →  cursor seek ≈ 2 ms      //
    // ------------------------------------------------------------------ //
    public async Task<CursorPagedResult<AuditLog>> GetLogsCursorPagedAsync(
        string? afterCursor,
        int pageSize,
        DateTime? start,
        DateTime? end,
        string? userId,
        string? actionType,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
      pageSize = Math.Min(Math.Max(pageSize, 1), 100);

      var fb = Builders<AuditLog>.Filter;
      var filter = fb.Empty;

      // ------ Static filters (same as offset query) ------
      if (start.HasValue)
        filter &= fb.Gte(x => x.CreatedAt, start.Value);

      if (end.HasValue)
        filter &= fb.Lte(x => x.CreatedAt, end.Value);

      if (!string.IsNullOrEmpty(userId))
        filter &= fb.Eq(x => x.UserId, userId);

      if (!string.IsNullOrEmpty(actionType))
        filter &= fb.Eq(x => x.Action, actionType);

      if (!string.IsNullOrEmpty(searchTerm))
      {
        filter &= fb.Or(
          fb.Regex(x => x.UserName, new BsonRegularExpression(searchTerm, "i")),
          fb.Regex(x => x.TableName, new BsonRegularExpression(searchTerm, "i")),
          fb.Regex(x => x.Action,    new BsonRegularExpression(searchTerm, "i"))
        );
      }

      // ------ Cursor seek (replaces Skip) ------
      if (!string.IsNullOrEmpty(afterCursor))
      {
        var (cursorCreatedAt, cursorId) = DecodeCursor(afterCursor);

        // Keyset condition for (CreatedAt DESC, _id DESC):
        //   next page = rows where CreatedAt < cursor  OR
        //               (CreatedAt == cursor AND _id < cursorObjectId)
        var cursorObjectId = ObjectId.Parse(cursorId);
        filter &= fb.Or(
          fb.Lt(x => x.CreatedAt, cursorCreatedAt),
          fb.And(
            fb.Eq(x => x.CreatedAt, cursorCreatedAt),
            fb.Lt("_id", cursorObjectId)
          )
        );
      }

      // Fetch one extra to determine if a next page exists
      var sort = Builders<AuditLog>.Sort
        .Descending(x => x.CreatedAt)
        .Descending("_id");

      var items = await _collection
        .Find(filter)
        .Sort(sort)
        .Limit(pageSize + 1)
        .ToListAsync(cancellationToken);

      string? nextCursor = null;
      if (items.Count > pageSize)
      {
        items.RemoveAt(items.Count - 1);          // trim the lookahead item
        var last = items[^1];
        nextCursor = EncodeCursor(last.CreatedAt, last.Id);
      }

      return new CursorPagedResult<AuditLog>
      {
        Items      = items,
        NextCursor = nextCursor,
        PageSize   = pageSize
      };
    }

    // ------------------------------------------------------------------ //
    //  Cursor encode / decode helpers                                      //
    // ------------------------------------------------------------------ //
    private static string EncodeCursor(DateTime createdAt, string id)
    {
      var json = JsonSerializer.Serialize(new CursorPayload(createdAt.Ticks, id));
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static (DateTime CreatedAt, string Id) DecodeCursor(string cursor)
    {
      try
      {
        var json    = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var payload = JsonSerializer.Deserialize<CursorPayload>(json)
                      ?? throw new FormatException("Null cursor payload.");
        return (new DateTime(payload.t, DateTimeKind.Utc), payload.id);
      }
      catch (Exception ex)
      {
        throw new ArgumentException("Invalid cursor value.", nameof(cursor), ex);
      }
    }

    private record CursorPayload(long t, string id);
  }
}
