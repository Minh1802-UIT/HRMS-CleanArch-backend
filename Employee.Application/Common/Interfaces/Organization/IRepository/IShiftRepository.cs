using System.Threading;
using Employee.Domain.Entities.Attendance;
using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IShiftRepository : IBaseRepository<Shift>
  {
    Task<Shift?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<Shift?> GetShiftByDateAsync(string employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<Shift>> GetAllActiveAsync(CancellationToken cancellationToken = default);
  }
}