using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.HumanResource;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.HumanResource
{
  public class JobVacancyRepository : BaseRepository<JobVacancy>, IJobVacancyRepository
  {
    public JobVacancyRepository(IMongoContext context) : base(context, "job_vacancies")
    {
    }

    public async Task<long> CountActiveAsync(CancellationToken cancellationToken = default) =>
        await _collection.CountDocumentsAsync(
            SoftDeleteFilter.GetActiveOnlyFilter<JobVacancy>(),
            cancellationToken: cancellationToken);
  }
}
