using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.AssignRole
{
    [Authorize(Roles = "Admin")]
public class AssignRoleCommand : IRequest
    {
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
}
