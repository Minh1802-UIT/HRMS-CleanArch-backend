using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Attendance;
using MongoDB.Driver;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Attendance
{
  public class RawAttendanceLogRepository : BaseRepository<RawAttendanceLog>, IRawAttendanceLogRepository
  {
    public RawAttendanceLogRepository(IMongoContext context) : base(context, "raw_attendance_logs")
    {
      // Index creation moved to MongoIndexInitializer for startup-only execution
    }

    public async Task<List<RawAttendanceLog>> GetAndLockUnprocessedLogsAsync(int batchSize = 100, CancellationToken cancellationToken = default)
    {
      // Atomic find-and-lock to prevent TOCTOU race condition:
      // Use FindOneAndUpdate in a loop to claim logs one at a time atomically
      var filter = Builders<RawAttendanceLog>.Filter.And(
          Builders<RawAttendanceLog>.Filter.Eq(x => x.IsProcessed, false),
          Builders<RawAttendanceLog>.Filter.Ne(x => x.ProcessingError, "PROCESSING")
      );

      var update = Builders<RawAttendanceLog>.Update
          .Set(x => x.ProcessingError, "PROCESSING")
          .Set(x => x.UpdatedAt, DateTime.UtcNow);

      var options = new FindOneAndUpdateOptions<RawAttendanceLog>
      {
        ReturnDocument = ReturnDocument.After,
        Sort = Builders<RawAttendanceLog>.Sort.Ascending(x => x.Timestamp)
      };

      var logs = new List<RawAttendanceLog>();
      for (int i = 0; i < batchSize; i++)
      {
        RawAttendanceLog? log;
        if (_context.Session != null)
          log = await _collection.FindOneAndUpdateAsync(_context.Session, filter, update, options, cancellationToken);
        else
          log = await _collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);

        if (log == null) break; // No more unprocessed logs
        logs.Add(log);
      }

      return logs;
    }

    public async Task MarkAsProcessedAsync(string id, CancellationToken cancellationToken = default)
    {
      var update = Builders<RawAttendanceLog>.Update
          .Set(x => x.IsProcessed, true)
          .Set(x => x.ProcessingError, "DONE")
          .Set(x => x.UpdatedAt, DateTime.UtcNow);

      if (_context.Session != null)
        await _collection.UpdateOneAsync(_context.Session, x => x.Id == id, update, cancellationToken: cancellationToken);
      else
        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task MarkAsErrorAsync(string id, string error, CancellationToken cancellationToken = default)
    {
      var update = Builders<RawAttendanceLog>.Update
          .Set(x => x.IsProcessed, false)
          .Set(x => x.ProcessingError, error)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);

      if (_context.Session != null)
        await _collection.UpdateOneAsync(_context.Session, x => x.Id == id, update, cancellationToken: cancellationToken);
      else
        await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }

    public async Task<RawAttendanceLog?> GetLatestLogAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.EmployeeId == employeeId)
          .SortByDescending(x => x.Timestamp)
          .FirstOrDefaultAsync(cancellationToken);
    }
  }
}
