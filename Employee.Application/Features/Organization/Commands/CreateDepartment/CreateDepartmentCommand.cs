using Employee.Application.Common.Security;
using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.CreateDepartment
{
  [Authorize(Roles = "Admin")]
public record CreateDepartmentCommand(CreateDepartmentDto Dto) : IRequest<DepartmentDto>;
}
