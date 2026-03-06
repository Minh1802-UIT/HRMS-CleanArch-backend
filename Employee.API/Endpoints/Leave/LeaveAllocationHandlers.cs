using Employee.API.Common;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Common.Models; // Add using
using Employee.Domain.Constants; // ErrorCodes
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Leave
{
  public static class LeaveAllocationHandlers
  {
    // 1. GET MY BALANCE
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
        [AsParameters] AllocationFilterDto dto,
        ILeaveAllocationService service)
    {
      var result = await service.GetAllAllocationsAsync(dto, dto.Keyword);
      return ResultUtils.Success(result, "Retrieved all leave allocations successfully.");
    }

    // 1.6. POST /list — paginated allocation list with body payload
    public static async Task<IResult> GetAllBalancesList(
        [FromBody] AllocationFilterDto dto,
        ILeaveAllocationService service)
    {
      var result = await service.GetAllAllocationsAsync(dto, dto.Keyword);
      return ResultUtils.Success(result, "Retrieved all leave allocations successfully.");
    }

    // 2. GET EMPLOYEE BALANCE (HR/Admin views another employee's balance)
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

    // 3. ALLOCATE / UPDATE (HR grants or adjusts leave days)

    public static async Task<IResult> Allocate(
        [FromBody] CreateAllocationDto dto,
        ILeaveAllocationService service)
    {
      // Create if absent, update if already exist (upsert handled by service)
      await service.AllocateDaysAsync(dto);
      return ResultUtils.Success($"Allocated {dto.NumberOfDays} days for employee successfully.");
    }

    // 4. DELETE ALLOCATION (Admin revokes leave days)
    public static async Task<IResult> Delete(string id, ILeaveAllocationService service)
    {
      await service.DeleteAsync(id);
      return ResultUtils.Success("Leave allocation removed successfully.");
    }

    // 5. INITIALIZE (Auto-initialize leave allocations for a new year)
    public static async Task<IResult> Initialize(
        int year,
        string employeeId,
        ILeaveAllocationService service)
    {
      await service.InitializeAllocationAsync(employeeId, year.ToString());
      return ResultUtils.Success("Leave allocation initialized successfully.");
    }

    // 6. CARRY FORWARD — Admin triggers year-end carry-forward
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
