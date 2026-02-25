using System.Threading;
using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces.Common;
using EmployeeEntity = Employee.Domain.Entities.HumanResource.EmployeeEntity;

namespace Employee.Application.Common.Interfaces.Organization.IRepository
{
  public interface IEmployeeRepository : IBaseRepository<EmployeeEntity>
  {
    Task<List<EmployeeEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    // New: Lookup for dropdowns (search-as-you-type)
    Task<List<LookupDto>> GetLookupAsync(string? keyword = null, int limit = 20, CancellationToken cancellationToken = default);

    // New: Optimized join - Get only names for specific IDs
    Task<Dictionary<string, (string Name, string Code)>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

    // New: Projection for PayrollProcessing - Get only active employee IDs
    Task<List<string>> GetActiveEmployeeIdsAsync(CancellationToken cancellationToken = default);

    // OPT-3: Direct filter by ManagerId (avoids loading all employees)
    Task<List<EmployeeEntity>> GetByManagerIdAsync(string managerId, CancellationToken cancellationToken = default);

    // IMP-1: Check references before delete
    Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
    Task<List<EmployeeEntity>> GetRecentHiresAsync(int count, CancellationToken cancellationToken = default);
    Task<List<EmployeeEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsByDepartmentIdAsync(string departmentId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByPositionIdAsync(string positionId, CancellationToken cancellationToken = default);
  }
}
