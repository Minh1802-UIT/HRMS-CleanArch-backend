using System.Threading;
using Employee.Domain.Entities.HumanResource;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
    public interface ICandidateRepository
    {
        Task<IEnumerable<Candidate>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Candidate?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Candidate>> GetByVacancyIdAsync(string vacancyId, CancellationToken cancellationToken = default);
        Task CreateAsync(Candidate entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(Candidate entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task ClearAllAsync(CancellationToken cancellationToken = default);
    }
}
