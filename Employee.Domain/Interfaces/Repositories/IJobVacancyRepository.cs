using System.Threading;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IJobVacancyRepository : IBaseRepository<JobVacancy>
  {
    Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
  }
}
