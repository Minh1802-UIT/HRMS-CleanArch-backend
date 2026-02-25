using System.Threading;
using Employee.Domain.Entities.HumanResource;
using Employee.Application.Common.Models;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IJobVacancyRepository
  {
    Task<IEnumerable<JobVacancy>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<JobVacancy?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task CreateAsync(JobVacancy entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobVacancy entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task<PagedResult<JobVacancy>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default);
    Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
  }
}
