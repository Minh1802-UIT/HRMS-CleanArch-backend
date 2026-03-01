using System.Threading;
using Employee.Domain.Entities.Performance;
using Employee.Domain.Interfaces.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IPerformanceGoalRepository : IBaseRepository<PerformanceGoal>
  {
    Task<IEnumerable<PerformanceGoal>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
  }
}
