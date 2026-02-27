using System.Threading;
using Employee.Domain.Entities.HumanResource;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IJobVacancyRepository : IBaseRepository<JobVacancy>
  {
    Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
  }
}
