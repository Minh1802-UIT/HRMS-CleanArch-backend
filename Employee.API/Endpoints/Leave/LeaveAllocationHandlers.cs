using Employee.API.Common;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Leave.Dtos;
using Employee.Application.Common.Models; // Add using
using Employee.Domain.Constants; // ErrorCodes
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Leave
{
  public static class LeaveAllocationHandlers
  {
    // 1. GET MY BALANCE (User xem số dư của mình)
    public static async Task<IResult> GetMyBalance(
        ILeaveAllocationService service,
        ICurrentUser currentUser)
    {
      if (string.IsNullOrEmpty(currentUser.EmployeeId))
      {
        return ResultUtils.Success(new List<LeaveAllocationDto>(), "User is not linked to any employee.");
      }

      var balance = await service.GetBalanceByEmployeeIdAsync(currentUser.EmployeeId);
      return ResultUtils.Success(balance, "Retrieved your leave balance successfully.");
    }

    // 1.5. GET ALL BALANCES (Report cho Admin/HR)
    public static async Task<IResult> GetAllBalances(
        [FromBody] AllocationFilterDto dto,
        ILeaveAllocationService service)
    {
      var result = await service.GetAllAllocationsAsync(dto, dto.Keyword);
      return ResultUtils.Success(result, "Retrieved all leave allocations successfully.");
    }

    // 2. GET EMPLOYEE BALANCE (HR/Admin xem số dư của nhân viên khác)
    public static async Task<IResult> GetBalanceByEmployee(
        string employeeId,
        ILeaveAllocationService service,
        ICurrentUser currentUser)
    {
      // Authorization
      var allowedRoles = new[] { "Admin", "HR" };
      var isManager = allowedRoles.Any(role => currentUser.IsInRole(role));
      var isOwner = currentUser.EmployeeId == employeeId;

      if (!isManager && !isOwner)
      {
        return ResultUtils.Fail(ErrorCodes.Forbidden, "You do not have permission to view allocations of other employees.");
      }

      var balance = await service.GetBalanceByEmployeeIdAsync(employeeId);
      return ResultUtils.Success(balance, "Retrieved employee leave balance successfully.");
    }

    // 3. ALLOCATE / UPDATE (HR cấp phép hoặc điều chỉnh số dư)
    // Đây là hàm quan trọng nhất: Create or Update Allocation
    public static async Task<IResult> Allocate(
        [FromBody] CreateAllocationDto dto,
        ILeaveAllocationService service)
    {
      // Logic: Nếu chưa có thì tạo mới, có rồi thì update (cộng dồn hoặc ghi đè tùy logic service)
      await service.AllocateDaysAsync(dto);
      return ResultUtils.Success($"Allocated {dto.NumberOfDays} days for employee successfully.");
    }

    // 4. DELETE ALLOCATION (Admin thu hồi phép - Ít dùng nhưng cần có)
    public static async Task<IResult> Delete(string id, ILeaveAllocationService service)
    {
      await service.DeleteAsync(id);
      return ResultUtils.Success("Leave allocation removed successfully.");
    }

    // 5. INITIALIZE (Tự động khởi tạo phép cho năm mới)
    public static async Task<IResult> Initialize(
        int year,
        string employeeId,
        ILeaveAllocationService service)
    {
      await service.InitializeAllocationAsync(employeeId, year.ToString());
      return ResultUtils.Success("Leave allocation initialized successfully.");
    }

    // 6. CARRY FORWARD (NEW-5) — Admin triggers year-end carry-forward
    // POST /api/leave-allocations/carry-forward/{fromYear}
    public static async Task<IResult> CarryForward(
        int fromYear,
        ILeaveAllocationService service)
    {
      if (fromYear < 2020 || fromYear > DateTime.UtcNow.Year)
        return ResultUtils.Fail("INVALID_YEAR", $"fromYear must be between 2020 and {DateTime.UtcNow.Year}.");

      var count = await service.RunYearEndCarryForwardAsync(fromYear);
      return ResultUtils.Success(
          count,
          $"Year-end carry-forward from {fromYear} to {fromYear + 1} completed. {count} allocation(s) updated.");
    }
  }
}