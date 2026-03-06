using Employee.Domain.Entities.Attendance;
using Employee.Domain.Enums;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IAttendanceExplanationRepository : IBaseRepository<AttendanceExplanation>
  {
    Task<List<AttendanceExplanation>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);

    /// <summary>Returns all pending explanations (for manager/HR review list).</summary>
    Task<List<AttendanceExplanation>> GetPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Get for a specific employee + date (prevent duplicate submissions).</summary>
    Task<AttendanceExplanation?> GetByEmployeeAndDateAsync(string employeeId, DateTime workDate, CancellationToken cancellationToken = default);
  }
}
