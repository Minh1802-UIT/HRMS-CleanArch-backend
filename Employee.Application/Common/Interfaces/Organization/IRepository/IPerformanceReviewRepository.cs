using System.Threading;
using Employee.Domain.Entities.Performance;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IPerformanceReviewRepository
  {
    Task<IEnumerable<PerformanceReview>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PerformanceReview?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task CreateAsync(PerformanceReview entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(PerformanceReview entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
  }
}
