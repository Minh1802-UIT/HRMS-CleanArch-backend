using System.Threading;
using Employee.Application.Common.Models;
using Employee.Domain.Entities.Leave;
using Employee.Application.Common.Interfaces.Common;

namespace Employee.Application.Common.Interfaces.Organization.IRepository;

public interface ILeaveRequestRepository : IBaseRepository<LeaveRequest>
{
  Task<List<LeaveRequest>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default);

  // New: Check Overlap (Phase 10)
  Task<long> CountByStatusAsync(Employee.Domain.Enums.LeaveStatus status, CancellationToken cancellationToken = default);
  Task<List<LeaveRequest>> GetRecentAsync(int count, CancellationToken cancellationToken = default);
  /// <summary>Check date overlap. Pass excludeId to ignore the request being updated (avoids self-conflict).</summary>
  Task<bool> ExistsOverlapAsync(string employeeId, DateTime from, DateTime to, string? excludeId = null, CancellationToken cancellationToken = default);
  Task DeleteByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default); // IMP-3
}
