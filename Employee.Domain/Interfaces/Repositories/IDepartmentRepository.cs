using System.Threading;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Common.Models;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IDepartmentRepository : IBaseRepository<Department>
  {
    // New: For optimized joins
    Task<Dictionary<string, string>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

    // New: Check if employee is a manager
    Task<bool> ExistsByManagerIdAsync(string managerId, CancellationToken cancellationToken = default);

    // Hierarchy
    Task<List<Department>> GetChildrenAsync(string parentId, CancellationToken cancellationToken = default);
    Task<List<Department>> GetAllActiveAsync(CancellationToken cancellationToken = default);
  }
}
