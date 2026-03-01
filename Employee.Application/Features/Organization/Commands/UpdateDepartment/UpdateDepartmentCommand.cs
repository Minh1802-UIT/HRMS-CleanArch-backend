using Employee.Application.Common.Security;
using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.UpdateDepartment
{
  [Authorize(Roles = "Admin")]
public record UpdateDepartmentCommand(string Id, UpdateDepartmentDto Dto) : IRequest;
}
