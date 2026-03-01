using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Common.Models;
// using Employee.Domain.Common.Models; // Removed duplicate

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface ILeaveAllocationService
  {
    // L?y danh sách s? du các lo?i phép c?a 1 nhân viên
    Task<IEnumerable<LeaveAllocationDto>> GetBalanceByEmployeeIdAsync(string employeeId);

    // L?y chi ti?t 1 lo?i phép (d? check balance khi t?o don)
    Task<LeaveAllocationDto?> GetByEmployeeAndTypeAsync(string employeeId, string leaveTypeId, string year);
    Task<PagedResult<LeaveAllocationDto>> GetAllAllocationsAsync(PaginationParams pagination, string? keyword = null);

    // C?p phép
    Task AllocateDaysAsync(CreateAllocationDto dto);

    // C?p nh?t s? ngày dã dùng (Deduct)
    Task UpdateUsedDaysAsync(string employeeId, string leaveTypeId, string year, double days);

    // Hoàn tr? ngày phép (Refund - khi h?y don)
    Task RefundDaysAsync(string employeeId, string leaveTypeId, string year, double days);

    Task InitializeAllocationAsync(string employeeId, string year);
    Task RunMonthlyAccrualAsync();

    /// <summary>
    /// NEW-5: Carry forward unused leave days from <paramref name="fromYear"/> to the next year
    /// for every leave type where AllowCarryForward=true, capped at MaxCarryForwardDays.
    /// </summary>
    Task<int> RunYearEndCarryForwardAsync(int fromYear);

    Task DeleteAsync(string id);
  }
}
