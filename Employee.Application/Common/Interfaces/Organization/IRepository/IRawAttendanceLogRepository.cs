using System.Threading;
using Employee.Domain.Entities.Attendance;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IRawAttendanceLogRepository : IBaseRepository<RawAttendanceLog>
  {
    Task<List<RawAttendanceLog>> GetAndLockUnprocessedLogsAsync(int batchSize = 100, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(string id, CancellationToken cancellationToken = default);
    Task MarkAsErrorAsync(string id, string error, CancellationToken cancellationToken = default);
    Task<RawAttendanceLog?> GetLatestLogAsync(string employeeId, CancellationToken cancellationToken = default);
  }
}