using System.Threading;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Interfaces.Repositories;

namespace Employee.Domain.Interfaces.Repositories;

public interface ILeaveRequestRepository : IBaseRepository<LeaveRequest>
{
  Task<List<LeaveRequest>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);

  // Check for date overlap; pass excludeId to ignore the request being updated
  Task<long> CountByStatusAsync(Employee.Domain.Enums.LeaveStatus status, CancellationToken cancellationToken = default);
  Task<List<LeaveRequest>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
  /// <summary>Check date overlap. Pass excludeId to ignore the request being updated (avoids self-conflict).</summary>
  Task<bool> ExistsOverlapAsync(string employeeId, DateTime from, DateTime to, string? excludeId = null, CancellationToken cancellationToken = default);
  Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);
}
