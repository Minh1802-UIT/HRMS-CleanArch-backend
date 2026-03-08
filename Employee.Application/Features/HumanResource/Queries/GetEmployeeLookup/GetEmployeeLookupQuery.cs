using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces;
using Employee.Domain.Enums;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeeLookup
{
  public record GetEmployeeLookupQuery(
      string? Keyword,
      int Limit = 20,
      string? DepartmentId = null,
      List<EmployeeStatus>? Statuses = null) : IRequest<List<LookupDto>>;
}
