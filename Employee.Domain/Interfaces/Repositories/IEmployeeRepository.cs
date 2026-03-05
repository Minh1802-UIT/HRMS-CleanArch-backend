using System.Threading;
using Employee.Domain.Common.Models;
using Employee.Domain.Interfaces.Repositories;
using EmployeeEntity = Employee.Domain.Entities.HumanResource.EmployeeEntity;

namespace Employee.Domain.Interfaces.Repositories
{
    public interface IEmployeeRepository : IBaseRepository<EmployeeEntity>
    {
        Task<List<EmployeeEntity>> GetAllIncludingDeletedAsync(CancellationToken cancellationToken = default);

        Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

        // Projection/DTO-returning methods are on IEmployeeQueryRepository (Application layer)
        // to avoid Domain interfaces depending on Application-layer types.

        // Optimized join - Get only names for specific IDs
        Task<Dictionary<string, (string Name, string Code)>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default);

        // Projection for payroll processing — fetches only active employee IDs
        Task<List<string>> GetActiveEmployeeIdsAsync(CancellationToken cancellationToken = default);

        // Direct filter by ManagerId (avoids loading all employees into memory)
        Task<List<EmployeeEntity>> GetByManagerIdAsync(string managerId, CancellationToken cancellationToken = default);

        // Check references before delete to prevent orphaned records
        Task<long> CountActiveAsync(CancellationToken cancellationToken = default);
        Task<List<EmployeeEntity>> GetRecentHiresAsync(int count, CancellationToken cancellationToken = default);
        Task<List<EmployeeEntity>> GetAllActiveAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsByDepartmentIdAsync(string departmentId, CancellationToken cancellationToken = default);
        Task<bool> ExistsByPositionIdAsync(string positionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Server-side aggregation: groups active employees by DepartmentId and returns counts.
        /// Avoids loading all employee documents into memory.
        /// </summary>
        Task<Dictionary<string, int>> GetDepartmentDistributionAsync(CancellationToken cancellationToken = default);
    }
}
