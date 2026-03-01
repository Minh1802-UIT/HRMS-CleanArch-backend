using System.Threading;
using Employee.Domain.Common.Models;

namespace Employee.Domain.Interfaces.Repositories
{
    public interface IBaseRepository<T> where T : class
    {
        Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<PagedResult<T>> GetPagedAsync(PaginationParams pagination, CancellationToken cancellationToken = default);
        Task CreateAsync(T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(string id, T entity, CancellationToken cancellationToken = default);
        Task UpdateAsync(string id, T entity, int expectedVersion, CancellationToken cancellationToken = default);
        Task DeleteAsync(string id, CancellationToken cancellationToken = default);
        Task ClearAllAsync(CancellationToken cancellationToken = default);
    }
}
