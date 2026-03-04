using Employee.API.Common;
using Employee.Domain.Constants;
using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Employee.Application.Features.HumanResource.Commands.CreateEmployee;
using Employee.Application.Features.HumanResource.Commands.UpdateEmployee;
using Employee.Application.Features.HumanResource.Commands.DeleteEmployee;
using Employee.Application.Features.HumanResource.Queries.GetEmployeesPaged;
using Employee.Application.Features.HumanResource.Queries.GetEmployeeLookup;
using Employee.Application.Features.HumanResource.Queries.GetEmployeeById;
using Employee.Application.Features.HumanResource.Queries.GetOrgChart;

namespace Employee.API.Endpoints.HumanResource
{
  public static class EmployeeHandlers
  {

    public static async Task<IResult> GetPagedList(
      [FromBody] PaginationParams pagination,
      ISender sender)
    {
      var result = await sender.Send(new GetEmployeesPagedQuery(pagination));
      return ResultUtils.Success(result, "Retrieved paginated employee list successfully.");
    }

    public static async Task<IResult> GetLookup(
      [FromQuery] string? keyword,
      [FromQuery] int? limit,
      ISender sender)
    {
      var result = await sender.Send(new GetEmployeeLookupQuery(keyword, limit is > 0 ? limit.Value : 20));
      return ResultUtils.Success(result, "Retrieved employee lookup successfully.");
    }

    // 2. GET BY ID
    public static async Task<IResult> GetById(string id, ISender sender)
    {
      var emp = await sender.Send(new GetEmployeeByIdQuery(id));
      return ResultUtils.Success(emp);
    }

    // 3. CREATE (CQRS - Using MediatR)
    public static async Task<IResult> Create([FromBody] CreateEmployeeDto dto, ISender sender)
    {
      var command = new CreateEmployeeCommand
      {
        EmployeeCode = dto.EmployeeCode,
        FullName = dto.FullName,
        Email = dto.Email,
        AvatarUrl = dto.AvatarUrl,
        PersonalInfo = dto.PersonalInfo,
        JobDetails = dto.JobDetails,
        BankDetails = dto.BankDetails
      };

      var result = await sender.Send(command);
      return ResultUtils.Created(result, "Employee created successfully via CQRS.");
    }

    // 4. UPDATE (CQRS - Using MediatR)
    public static async Task<IResult> Update(string id, [FromBody] UpdateEmployeeDto dto, ISender sender)
    {
      if (id != dto.Id)
      {
        return ResultUtils.Fail(ErrorCodes.InvalidData, "DevLog: URL ID mismatch.");
      }

      var command = new UpdateEmployeeCommand
      {
        Id = dto.Id,
        FullName = dto.FullName,
        Email = dto.Email,
        AvatarUrl = dto.AvatarUrl,
        Version = dto.Version,
        PersonalInfo = dto.PersonalInfo,
        JobDetails = dto.JobDetails,
        BankDetails = dto.BankDetails
      };

      await sender.Send(command);
      return ResultUtils.Success("Employee updated successfully via CQRS.");
    }

    // 5. DELETE (CQRS - Using MediatR)
    public static async Task<IResult> Delete(string id, ISender sender)
    {
      var command = new DeleteEmployeeCommand(id);
      await sender.Send(command);

      return ResultUtils.Success("Employee deleted successfully via CQRS.");
    }

    // 6. ORG CHART (Person-based hierarchy)
    public static async Task<IResult> GetOrgChart(ISender sender)
    {
      var chart = await sender.Send(new GetOrgChartQuery());
      return ResultUtils.Success(chart, "Retrieved employee organizational chart successfully.");
    }
  }
}
