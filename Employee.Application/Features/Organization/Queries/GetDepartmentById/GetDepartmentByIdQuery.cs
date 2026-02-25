using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Queries.GetDepartmentById
{
  public record GetDepartmentByIdQuery(string Id) : IRequest<DepartmentDto>;
}
