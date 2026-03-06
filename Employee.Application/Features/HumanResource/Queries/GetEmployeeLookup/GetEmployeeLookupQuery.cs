using Employee.Application.Common.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.HumanResource.Queries.GetEmployeeLookup
{
  public record GetEmployeeLookupQuery(string? Keyword, int Limit = 20, string? DepartmentId = null) : IRequest<List<LookupDto>>;
}
