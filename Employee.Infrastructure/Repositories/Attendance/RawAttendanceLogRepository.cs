using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Domain.Interfaces.Repositories;
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

    public async Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      // Hard-delete: raw logs are transient data with no audit requirement after employee removal.
      var filter = Builders<RawAttendanceLog>.Filter.Eq(x => x.EmployeeId, employeeId);
      if (_context.Session != null)
        await _collection.DeleteManyAsync(_context.Session, filter, cancellationToken: cancellationToken);
      else
        await _collection.DeleteManyAsync(filter, cancellationToken: cancellationToken);
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

    /// <summary>
    /// Marks multiple raw logs as processed in a single BulkWriteAsync call,
    /// reducing N round-trips (one UpdateOne per log) to exactly 1.
    /// Automatically enrolls in the ambient MongoDB session when one is active.
    /// </summary>
    public async Task MarkManyAsProcessedAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
      var idList = ids.ToList();
      if (idList.Count == 0) return;

      var now = DateTime.UtcNow;
      var bulkOps = idList.Select(id =>
      {
        var filter = Builders<RawAttendanceLog>.Filter.Eq(x => x.Id, id);
        var update = Builders<RawAttendanceLog>.Update
            .Set(x => x.IsProcessed,    true)
            .Set(x => x.ProcessingError, "DONE")
            .Set(x => x.UpdatedAt,       now);
        return (WriteModel<RawAttendanceLog>)new UpdateOneModel<RawAttendanceLog>(filter, update);
      }).ToList();

      var options = new BulkWriteOptions { IsOrdered = false };

      if (_context.Session != null)
        await _collection.BulkWriteAsync(_context.Session, bulkOps, options, cancellationToken);
      else
        await _collection.BulkWriteAsync(bulkOps, options, cancellationToken);
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

    public async Task<List<RawAttendanceLog>> GetByDateRangeAsync(
        string employeeId,
        DateTime startUtc,
        DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
      var filter = Builders<RawAttendanceLog>.Filter.And(
          Builders<RawAttendanceLog>.Filter.Eq(x => x.EmployeeId, employeeId),
          Builders<RawAttendanceLog>.Filter.Gte(x => x.Timestamp, startUtc),
          Builders<RawAttendanceLog>.Filter.Lt(x => x.Timestamp, endUtc)
      );
      return await _collection.Find(filter)
          .SortBy(x => x.Timestamp)
          .ToListAsync(cancellationToken);
    }

    public async Task<long> ResetProcessingStatusAsync(
        DateTime startUtc, DateTime endUtc,
        CancellationToken cancellationToken = default)
    {
      var filter = Builders<RawAttendanceLog>.Filter.And(
          Builders<RawAttendanceLog>.Filter.Gte(x => x.Timestamp, startUtc),
          Builders<RawAttendanceLog>.Filter.Lt(x => x.Timestamp, endUtc),
          Builders<RawAttendanceLog>.Filter.Eq(x => x.IsProcessed, true));

      var update = Builders<RawAttendanceLog>.Update
          .Set(x => x.IsProcessed, false)
          .Set(x => x.ProcessingError, (string?)null)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);

      var result = await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
      return result.ModifiedCount;
    }
  }
}
