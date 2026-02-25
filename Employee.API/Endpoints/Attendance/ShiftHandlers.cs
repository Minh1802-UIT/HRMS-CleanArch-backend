using Employee.API.Common;
using Employee.Domain.Constants;
using Employee.Application.Features.Attendance.Dtos;
using Employee.Application.Common.Interfaces.Attendance.IService;
using Employee.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Employee.API.Endpoints.Attendance
{
  public static class ShiftHandlers
  {
    // 1. GET PAGED
    public static async Task<IResult> GetPaged(
        [AsParameters] PaginationParams pagination,
        IShiftService service)
    {
      var result = await service.GetPagedAsync(pagination);
      return ResultUtils.Success(result);
    }

    // 1b. GET LOOKUP (For Dropdowns)
    public static async Task<IResult> GetLookup(IShiftService service)
    {
      var list = await service.GetLookupAsync();
      return ResultUtils.Success(list);
    }

    // 2. GET BY ID
    public static async Task<IResult> GetById(string id, IShiftService service)
    {
      var item = await service.GetByIdAsync(id);
      if (item == null)
      {
        return ResultUtils.Fail(ErrorCodes.NotFound("SHIFT"), $"DevLog: Shift {id} not found.");
      }
      return ResultUtils.Success(item);
    }

    // 3. CREATE
    public static async Task<IResult> Create([FromBody] CreateShiftDto dto, IShiftService service)
    {
      var id = await service.CreateAsync(dto);
      return ResultUtils.Created(id, "Shift configuration created successfully.");
    }

    // 4. UPDATE
    public static async Task<IResult> Update(string id, [FromBody] UpdateShiftDto dto, IShiftService service)
    {
      if (id != dto.Id)
      {
        return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: URL ID mismatch.");
      }

      await service.UpdateAsync(id, dto);
      return ResultUtils.Success("Shift configuration updated successfully.");
    }

    // 5. DELETE
    public static async Task<IResult> Delete(string id, IShiftService service)
    {
      await service.DeleteAsync(id);
      return ResultUtils.Success("Shift configuration deleted successfully.");
    }
  }
}