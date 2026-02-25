using System.Threading;
using Employee.Domain.Entities.Performance;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IPerformanceGoalRepository
  {
    Task<IEnumerable<PerformanceGoal>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PerformanceGoal?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceGoal>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task CreateAsync(PerformanceGoal entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(PerformanceGoal entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
  }
}
