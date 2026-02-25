using Employee.Application.Features.HumanResource.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.HumanResource.Queries.GetOrgChart
{
  public record GetOrgChartQuery() : IRequest<List<EmployeeOrgNodeDto>>;
}
