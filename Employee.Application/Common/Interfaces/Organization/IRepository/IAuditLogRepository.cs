using System.Threading;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.Common;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IAuditLogRepository : IBaseRepository<AuditLog>
  {
    Task<List<AuditLog>> GetLogsAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>Offset-based paged query (kept for backward compatibility).</summary>
    Task<(List<AuditLog> Logs, long TotalCount)> GetLogsPagedAsync(PaginationParams pagination, DateTime? start, DateTime? end, string? userId, string? actionType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cursor-based (keyset) paged query.
    /// Solves the O(N) Skip cost on large collections (250 K+ docs).
    /// Sort order: CreatedAt DESC, _id DESC.
    /// Pass <paramref name="afterCursor"/> = null for the first page;
    /// on subsequent pages pass the <c>NextCursor</c> from the previous result.
    /// </summary>
    Task<CursorPagedResult<AuditLog>> GetLogsCursorPagedAsync(
        string? afterCursor,
        int pageSize,
        DateTime? start,
        DateTime? end,
        string? userId,
        string? actionType,
        string? searchTerm,
        CancellationToken cancellationToken = default);
  }
}
