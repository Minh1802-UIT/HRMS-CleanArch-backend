using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Common.Models;

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface ILeaveAllocationService
  {
    // Retrieve all leave balances for an employee
    Task<IEnumerable<LeaveAllocationDto>> GetBalanceByEmployeeIdAsync(string employeeId);

    // Get balance for a specific leave type (used when submitting a request)
    Task<LeaveAllocationDto?> GetByEmployeeAndTypeAsync(string employeeId, string leaveTypeId, string year);
    Task<PagedResult<LeaveAllocationDto>> GetAllAllocationsAsync(PaginationParams pagination, string? keyword = null);

    // Grant / adjust leave days
    Task AllocateDaysAsync(CreateAllocationDto dto);

    // Deduct used days
    Task UpdateUsedDaysAsync(string employeeId, string leaveTypeId, string year, double days);

    // Refund leave days (called on request cancellation)
    Task RefundDaysAsync(string employeeId, string leaveTypeId, string year, double days);

    Task InitializeAllocationAsync(string employeeId, string year);
    Task RunMonthlyAccrualAsync();

    /// <summary>
    /// Carry forward unused leave days from <paramref name="fromYear"/> to the next year
    /// for every leave type where AllowCarryForward=true, capped at MaxCarryForwardDays.
    /// </summary>
    Task<int> RunYearEndCarryForwardAsync(int fromYear);

    Task DeleteAsync(string id);
  }
}
