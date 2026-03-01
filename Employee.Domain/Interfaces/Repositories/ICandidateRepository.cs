using System.Threading;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface ICandidateRepository : IBaseRepository<Candidate>
  {
        Task<IEnumerable<Candidate>> GetByVacancyIdAsync(string vacancyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Server-side aggregation: groups candidates by Status and returns counts.
    /// </summary>
    Task<Dictionary<string, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);
    }
}
