using Employee.API.Common;
using Employee.Application.Common.Interfaces.Organization.IService; // Correct Namespace
using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Constants;
using Employee.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Leave
{
  public static class LeaveTypeHandlers
  {
    // 1. GET PAGED
    public static async Task<IResult> GetPaged(
        [AsParameters] PaginationParams pagination,
        ILeaveTypeService service)
    {
      var result = await service.GetPagedAsync(pagination);
      return ResultUtils.Success(result, "Retrieved leave types successfully.");
    }

    // 2. GET BY ID
    public static async Task<IResult> GetById(string id, ILeaveTypeService service)
    {
      var item = await service.GetByIdAsync(id);
      if (item == null)
      {
        return ResultUtils.Fail(ErrorCodes.NotFound("LEAVE_TYPE"), $"DevLog: Leave Type {id} not found.");
      }
      return ResultUtils.Success(item);
    }

    // 3. CREATE (Admin Only)
    public static async Task<IResult> Create([FromBody] CreateLeaveTypeDto dto, ILeaveTypeService service)
    {
      await service.CreateAsync(dto); // No return value
      return ResultUtils.Created(string.Empty, "Leave type created successfully.");
    }

    // 4. UPDATE (Admin Only)
    public static async Task<IResult> Update(string id, [FromBody] UpdateLeaveTypeDto dto, ILeaveTypeService service)
    {
      if (id != dto.Id)
      {
        return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: URL ID mismatch.");
      }

      await service.UpdateAsync(id, dto);
      return ResultUtils.Success("Leave type updated successfully.");
    }

    // 5. DELETE (Admin Only)
    public static async Task<IResult> Delete(string id, ILeaveTypeService service)
    {
      await service.DeleteAsync(id);
      return ResultUtils.Success("Leave type deleted successfully.");
    }
  }
}