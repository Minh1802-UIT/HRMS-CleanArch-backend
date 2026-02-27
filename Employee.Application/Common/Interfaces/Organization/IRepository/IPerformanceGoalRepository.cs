using System.Threading;
using Employee.Domain.Entities.Performance;
using Employee.Application.Common.Interfaces.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IPerformanceGoalRepository : IBaseRepository<PerformanceGoal>
  {
    Task<IEnumerable<PerformanceGoal>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
  }
}
