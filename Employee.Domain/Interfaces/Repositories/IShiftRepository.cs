using System.Threading;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Common.Models;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IShiftRepository : IBaseRepository<Shift>
  {
    Task<Shift?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Shift?> GetShiftByDateAsync(string employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<Shift>> GetAllActiveAsync(CancellationToken cancellationToken = default);
  }
}
