using System.Threading;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Common.Models;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface ILeaveAllocationRepository : IBaseRepository<LeaveAllocation>
  {
    Task<LeaveAllocation?> GetByEmployeeAndTypeAsync(string employeeId, string leaveTypeId, string year, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveAllocation>> GetByEmployeeAsync(string employeeId, string year, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveAllocation>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<PagedResult<LeaveAllocation>> GetPagedAsync(PaginationParams pagination, List<string>? employeeIds = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<LeaveAllocation>> GetByEmployeeIdsAndYearAsync(List<string> employeeIds, string year, CancellationToken cancellationToken = default);
    Task BulkUpsertAsync(List<LeaveAllocation> allocations, CancellationToken cancellationToken = default);
    Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
  }
}
