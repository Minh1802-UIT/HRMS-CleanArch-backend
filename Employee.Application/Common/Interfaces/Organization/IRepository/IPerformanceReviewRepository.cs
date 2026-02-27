using System.Threading;
using Employee.Domain.Entities.Performance;
using Employee.Application.Common.Interfaces.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IPerformanceReviewRepository : IBaseRepository<PerformanceReview>
  {
    Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
  }
}
