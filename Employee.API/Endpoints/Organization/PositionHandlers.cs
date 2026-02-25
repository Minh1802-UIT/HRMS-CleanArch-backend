using Employee.API.Common; // ResultUtils
using Employee.Domain.Constants; // ErrorCodes
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Queries.GetPositionsPaged;
using Employee.Application.Features.Organization.Queries.GetPositionById;
using Employee.Application.Features.Organization.Queries.GetPositionTree;
using Employee.Application.Features.Organization.Commands.CreatePosition;
using Employee.Application.Features.Organization.Commands.UpdatePosition;
using Employee.Application.Features.Organization.Commands.DeletePosition;
using Employee.Application.Common.Models;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Organization
{
  public static class PositionHandlers
  {
    // 1. GET PAGED
    public static async Task<IResult> GetPaged(
        [AsParameters] PaginationParams pagination,
        ISender sender)
    {
      var result = await sender.Send(new GetPositionsPagedQuery(pagination));
      return ResultUtils.Success(result);
    }

    public static async Task<IResult> GetTree(ISender sender)
    {
      var tree = await sender.Send(new GetPositionTreeQuery());
      return ResultUtils.Success(tree);
    }

    // 2. GET BY ID
    public static async Task<IResult> GetById(string id, ISender sender)
    {
      var item = await sender.Send(new GetPositionByIdQuery(id));
      return ResultUtils.Success(item);
    }

    // 3. CREATE
    public static async Task<IResult> Create([FromBody] CreatePositionDto dto, ISender sender)
    {
      var id = await sender.Send(new CreatePositionCommand(dto));
      return ResultUtils.Created(id, "Position created successfully.");
    }

    // 4. UPDATE
    public static async Task<IResult> Update(string id, [FromBody] UpdatePositionDto dto, ISender sender)
    {
      // Validate ID trên URL và Body phải khớp nhau
      if (id != dto.Id)
      {
        return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: URL ID mismatch.");
      }

      await sender.Send(new UpdatePositionCommand(id, dto));
      return ResultUtils.Success("Position updated successfully.");
    }

    // 5. DELETE
    public static async Task<IResult> Delete(string id, ISender sender)
    {
      await sender.Send(new DeletePositionCommand(id));
      return ResultUtils.Success("Position deleted successfully.");
    }
  }
}
