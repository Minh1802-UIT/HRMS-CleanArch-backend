using System.Threading;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.Common;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IAuditLogRepository : IBaseRepository<AuditLog>
  {
    Task<List<AuditLog>> GetLogsAsync(int limit, CancellationToken cancellationToken = default);
    Task<(List<AuditLog> Logs, long TotalCount)> GetLogsPagedAsync(PaginationParams pagination, DateTime? start, DateTime? end, string? userId, string? actionType, CancellationToken cancellationToken = default);
  }
}
