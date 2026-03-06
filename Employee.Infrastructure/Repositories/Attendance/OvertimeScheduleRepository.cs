using Employee.Domain.Entities.Attendance;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Attendance
{
  public class OvertimeScheduleRepository
      : BaseRepository<OvertimeSchedule>, IOvertimeScheduleRepository
  {
    public OvertimeScheduleRepository(IMongoContext context)
        : base(context, "overtime_schedules") { }

    public async Task<bool> ExistsAsync(
        string employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
      var filter = Builders<OvertimeSchedule>.Filter.And(
          Builders<OvertimeSchedule>.Filter.Eq(x => x.EmployeeId, employeeId),
          Builders<OvertimeSchedule>.Filter.Eq(x => x.Date, date.Date));
      return await _collection.Find(filter).AnyAsync(cancellationToken);
    }

    public async Task<List<OvertimeSchedule>> GetByDateRangeAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
      var filter = Builders<OvertimeSchedule>.Filter.And(
          Builders<OvertimeSchedule>.Filter.Gte(x => x.Date, from.Date),
          Builders<OvertimeSchedule>.Filter.Lte(x => x.Date, to.Date));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<List<OvertimeSchedule>> GetByEmployeeAndMonthAsync(
        string employeeId, string monthKey, CancellationToken cancellationToken = default)
    {
      // monthKey = "MM-yyyy"
      var parts = monthKey.Split('-');
      var month = int.Parse(parts[0]);
      var year  = int.Parse(parts[1]);
      var from  = new DateTime(year, month, 1);
      var to    = from.AddMonths(1).AddDays(-1);

      var filter = Builders<OvertimeSchedule>.Filter.And(
          Builders<OvertimeSchedule>.Filter.Eq(x => x.EmployeeId, employeeId),
          Builders<OvertimeSchedule>.Filter.Gte(x => x.Date, from),
          Builders<OvertimeSchedule>.Filter.Lte(x => x.Date, to));
      return await _collection.Find(filter)
          .SortBy(x => x.Date)
          .ToListAsync(cancellationToken);
    }

    public async Task<List<OvertimeSchedule>> GetByMonthAsync(
        string monthKey, CancellationToken cancellationToken = default)
    {
      var parts = monthKey.Split('-');
      var month = int.Parse(parts[0]);
      var year  = int.Parse(parts[1]);
      var from  = new DateTime(year, month, 1);
      var to    = from.AddMonths(1).AddDays(-1);

      var filter = Builders<OvertimeSchedule>.Filter.And(
          Builders<OvertimeSchedule>.Filter.Gte(x => x.Date, from),
          Builders<OvertimeSchedule>.Filter.Lte(x => x.Date, to));
      return await _collection.Find(filter)
          .SortBy(x => x.EmployeeId).SortBy(x => x.Date)
          .ToListAsync(cancellationToken);
    }
  }
}
