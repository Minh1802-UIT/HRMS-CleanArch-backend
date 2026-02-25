using Employee.Application.Features.Leave.Dtos;
using Employee.Application.Common.Models;
// using Employee.Application.Common.Models; // Removed duplicate

namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface ILeaveAllocationService
  {
    // Lấy danh sách số dư các loại phép của 1 nhân viên
    Task<IEnumerable<LeaveAllocationDto>> GetBalanceByEmployeeIdAsync(string employeeId);

    // Lấy chi tiết 1 loại phép (để check balance khi tạo đơn)
    Task<LeaveAllocationDto?> GetByEmployeeAndTypeAsync(string employeeId, string leaveTypeId, string year);
    Task<PagedResult<LeaveAllocationDto>> GetAllAllocationsAsync(PaginationParams pagination, string? keyword = null);

    // Cấp phép
    Task AllocateDaysAsync(CreateAllocationDto dto);

    // Cập nhật số ngày đã dùng (Deduct)
    Task UpdateUsedDaysAsync(string employeeId, string leaveTypeId, string year, double days);

    // Hoàn trả ngày phép (Refund - khi hủy đơn)
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
