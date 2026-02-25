using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.CreateDepartment
{
  public record CreateDepartmentCommand(CreateDepartmentDto Dto) : IRequest<DepartmentDto>;
}
