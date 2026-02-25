using System.Threading;
using Employee.Domain.Entities.Attendance;
using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IAttendanceRepository : IBaseRepository<AttendanceBucket>
  {
    Task<AttendanceBucket?> GetByEmployeeAndMonthAsync(string employeeId, string month, CancellationToken cancellationToken = default);

    // 4. Lấy danh sách bucket cho 1 tháng (để xử lý chốt công)
    Task<IEnumerable<AttendanceBucket>> GetByMonthAsync(string month, CancellationToken cancellationToken = default);
    Task<IEnumerable<AttendanceBucket>> GetByMonthsAsync(IEnumerable<string> months, CancellationToken cancellationToken = default);

    Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default); // IMP-3
  }
}
