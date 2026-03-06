using Employee.Domain.Entities.Attendance;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IOvertimeScheduleRepository : IBaseRepository<OvertimeSchedule>
  {
    /// <summary>Check whether a specific employee has an approved OT entry on a given date.</summary>
    Task<bool> ExistsAsync(string employeeId, DateTime date, CancellationToken cancellationToken = default);

    /// <summary>Get OT schedule entries for a date range (used by the processing service to batch-load).</summary>
    Task<List<OvertimeSchedule>> GetByDateRangeAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);

    /// <summary>Get all OT schedules for one employee within a month key ("MM-yyyy").</summary>
    Task<List<OvertimeSchedule>> GetByEmployeeAndMonthAsync(string employeeId, string monthKey, CancellationToken cancellationToken = default);

    /// <summary>Get all OT schedules across all employees for a given month key (admin view).</summary>
    Task<List<OvertimeSchedule>> GetByMonthAsync(string monthKey, CancellationToken cancellationToken = default);
  }
}
