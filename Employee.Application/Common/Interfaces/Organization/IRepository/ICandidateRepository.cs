using System.Threading;
using Employee.Domain.Entities.HumanResource;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
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
