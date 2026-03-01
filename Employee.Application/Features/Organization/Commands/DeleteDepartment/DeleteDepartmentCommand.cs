using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Organization.Commands.DeleteDepartment
{
  [Authorize(Roles = "Admin")]
public record DeleteDepartmentCommand(string Id) : IRequest;
}
