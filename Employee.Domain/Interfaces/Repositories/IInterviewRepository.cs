using System.Threading;
using Employee.Domain.Entities.HumanResource;

namespace Employee.Domain.Interfaces.Repositories
{
    public interface IInterviewRepository
    {
        Task<IEnumerable<Interview>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Interview>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default);
        Task<IEnumerable<Interview>> GetByCandidateIdAsync(string candidateId, CancellationToken cancellationToken = default);
        Task<Interview?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task CreateAsync(Interview entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(Interview entity, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task ClearAllAsync(CancellationToken cancellationToken = default);
    }
}
