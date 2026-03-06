using Employee.API.Common;
using Employee.Application.Features.Organization.Dtos;
using Employee.Application.Features.Organization.Queries.GetDepartmentsPaged;
using Employee.Application.Features.Organization.Queries.GetDepartmentById;
using Employee.Application.Features.Organization.Queries.GetDepartmentTree;
using Employee.Application.Features.Organization.Commands.CreateDepartment;
using Employee.Application.Features.Organization.Commands.UpdateDepartment;
using Employee.Application.Features.Organization.Commands.DeleteDepartment;
using Employee.Domain.Constants;
using Employee.Domain.Common.Models;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Employee.API.Endpoints.Organization;

public static class DepartmentHandlers
{
  public static async Task<IResult> GetPaged(
      [AsParameters] PaginationParams pagination,
      ISender sender)
  {
    var result = await sender.Send(new GetDepartmentsPagedQuery(pagination));
    return ResultUtils.Success(result);
  }

  public static async Task<IResult> GetTree(ISender sender)
  {
    var tree = await sender.Send(new GetDepartmentTreeQuery());
    return ResultUtils.Success(tree);
  }

  public static async Task<IResult> GetById(string id, ISender sender)
  {
    var dept = await sender.Send(new GetDepartmentByIdQuery(id));
    return ResultUtils.Success(dept);
  }

  public static async Task<IResult> Create([FromBody] CreateDepartmentDto dto, ISender sender)
  {
    var result = await sender.Send(new CreateDepartmentCommand(dto));
    return ResultUtils.Created(result.Id, "Created successfully", $"/api/departments/{result.Id}");
  }

  public static async Task<IResult> Update(string id, [FromBody] UpdateDepartmentDto dto, ISender sender)
  {
    if (id != dto.Id)
      return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: ID Mismatch");

    await sender.Send(new UpdateDepartmentCommand(id, dto));
    return ResultUtils.Success("Updated successfully");
  }

  public static async Task<IResult> Delete(string id, ISender sender)
  {
    await sender.Send(new DeleteDepartmentCommand(id));
    return ResultUtils.Success("Deleted successfully");
  }
}
