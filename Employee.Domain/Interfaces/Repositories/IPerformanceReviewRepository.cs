using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Performance;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IPerformanceReviewRepository : IBaseRepository<PerformanceReview>
  {
    Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<PerformanceReviewStats> GetCompletedStatsAsync(CancellationToken cancellationToken = default);
    Task<List<EmployeeScoreAggregate>> GetAtRiskEmployeesAsync(int top, CancellationToken cancellationToken = default);
  }
}
