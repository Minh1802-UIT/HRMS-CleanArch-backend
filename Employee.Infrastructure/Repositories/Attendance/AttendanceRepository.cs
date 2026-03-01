using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Attendance;
using MongoDB.Driver;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Attendance
{
  public class AttendanceRepository : BaseRepository<AttendanceBucket>, IAttendanceRepository
  {
    public AttendanceRepository(IMongoContext context) : base(context, "attendance_buckets")
    {
    }

    public async Task<AttendanceBucket?> GetByEmployeeAndMonthAsync(string employeeId, string month, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.EmployeeId == employeeId && x.Month == month && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);

    public async Task<IEnumerable<AttendanceBucket>> GetByMonthAsync(string month, CancellationToken cancellationToken = default) =>
        await _collection.Find(x => x.Month == month && x.IsDeleted != true).ToListAsync(cancellationToken);

    public async Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var update = Builders<AttendanceBucket>.Update
          .Set(x => x.IsDeleted, true)
          .Set(x => x.UpdatedAt, DateTime.UtcNow);
      await _collection.UpdateManyAsync(x => x.EmployeeId == employeeId && x.IsDeleted != true, update, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<AttendanceBucket>> GetByMonthsAsync(IEnumerable<string> months, CancellationToken cancellationToken = default)
    {
      var filter = Builders<AttendanceBucket>.Filter.In(x => x.Month, months) &
                   Builders<AttendanceBucket>.Filter.Ne(x => x.IsDeleted, true);
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
  }
}
