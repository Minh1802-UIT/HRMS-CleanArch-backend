using Employee.Application.Features.Organization.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Organization.Queries.GetDepartmentTree
{
  public record GetDepartmentTreeQuery() : IRequest<List<DepartmentNodeDto>>;
}
