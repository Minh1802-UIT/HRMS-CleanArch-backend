using System.Threading;
using Employee.Domain.Entities.Attendance;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IRawAttendanceLogRepository : IBaseRepository<RawAttendanceLog>
  {
    Task<List<RawAttendanceLog>> GetAndLockUnprocessedLogsAsync(int batchSize = 100, CancellationToken cancellationToken = default);

    /// <summary>Single-document update (used when only one log needs marking).</summary>
    Task MarkAsProcessedAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch update — replaces N individual UpdateOneAsync round-trips with a
    /// single BulkWriteAsync, reducing DB round-trips from N to 1.
    /// </summary>
    Task MarkManyAsProcessedAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);

    Task MarkAsErrorAsync(string id, string error, CancellationToken cancellationToken = default);
    Task<RawAttendanceLog?> GetLatestLogAsync(string employeeId, CancellationToken cancellationToken = default);
  }
}
