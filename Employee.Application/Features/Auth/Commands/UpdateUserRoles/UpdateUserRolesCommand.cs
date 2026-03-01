using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.UpdateUserRoles
{
    [Authorize(Roles = "Admin")]
public class UpdateUserRolesCommand : IRequest
    {
        public string UserId { get; set; } = string.Empty;
        public List<string> RoleNames { get; set; } = new();
    }
}
