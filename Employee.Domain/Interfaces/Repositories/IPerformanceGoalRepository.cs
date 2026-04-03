using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Performance;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IPerformanceGoalRepository : IBaseRepository<PerformanceGoal>
  {
    Task<IEnumerable<PerformanceGoal>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<long> CountAllAsync(CancellationToken cancellationToken = default);
    Task<long> CountByStatusAsync(PerformanceGoalStatus status, CancellationToken cancellationToken = default);
    Task<double> GetAverageProgressAsync(PerformanceGoalStatus status, CancellationToken cancellationToken = default);
    Task<List<MonthlyGoalStatsAggregate>> GetMonthlyStatsAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<long> MarkOverdueAsync(DateTime utcNow, CancellationToken cancellationToken = default);
  }
}
