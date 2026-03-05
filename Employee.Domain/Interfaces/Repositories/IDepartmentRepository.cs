using System.Threading;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Common.Models;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IDepartmentRepository : IBaseRepository<Department>
  {
    // For optimized joins: resolve names from IDs without loading full documents
    Task<Dictionary<string, string>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

    // Returns true if any department lists the given employee as its manager
    Task<bool> ExistsByManagerIdAsync(string managerId, CancellationToken cancellationToken = default);

    // Hierarchy
    Task<List<Department>> GetChildrenAsync(string parentId, CancellationToken cancellationToken = default);
    Task<List<Department>> GetAllActiveAsync(CancellationToken cancellationToken = default);
  }
}
