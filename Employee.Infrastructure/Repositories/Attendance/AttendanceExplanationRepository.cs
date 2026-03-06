using Employee.Application.Common.Interfaces;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Attendance
{
  public class AttendanceExplanationRepository
      : BaseRepository<AttendanceExplanation>, IAttendanceExplanationRepository
  {
    public AttendanceExplanationRepository(IMongoContext context)
        : base(context, "attendance_explanations") { }

    public async Task<List<AttendanceExplanation>> GetByEmployeeIdAsync(
        string employeeId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<AttendanceExplanation>.Filter.Eq(x => x.EmployeeId, employeeId);
      var sort = Builders<AttendanceExplanation>.Sort.Descending(x => x.WorkDate);
      return await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
    }

    public async Task<List<AttendanceExplanation>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
      var filter = Builders<AttendanceExplanation>.Filter.Eq(x => x.Status, ExplanationStatus.Pending);
      var sort = Builders<AttendanceExplanation>.Sort.Ascending(x => x.CreatedAt);
      return await _collection.Find(filter).Sort(sort).ToListAsync(cancellationToken);
    }

    public async Task<AttendanceExplanation?> GetByEmployeeAndDateAsync(
        string employeeId, DateTime workDate, CancellationToken cancellationToken = default)
    {
      var filter = Builders<AttendanceExplanation>.Filter.And(
          Builders<AttendanceExplanation>.Filter.Eq(x => x.EmployeeId, employeeId),
          Builders<AttendanceExplanation>.Filter.Eq(x => x.WorkDate, workDate.Date));
      return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }
  }
}
