using System.Collections.Generic;
using System.Threading;
using Employee.Domain.Entities.Performance;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IPIPRepository : IBaseRepository<PIP>
  {
    Task<IEnumerable<PIP>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PIP>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PIP>> GetByManagerIdAsync(string managerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PIP>> GetByStatusAsync(int status, CancellationToken cancellationToken = default);
    Task<IEnumerable<PIP>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<long> CountByStatusAsync(PIPStatus status, CancellationToken cancellationToken = default);
  }
}
