using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.CreateRole
{
    [Authorize(Roles = "Admin")]
public class CreateRoleCommand : IRequest<string>
    {
        public string RoleName { get; set; } = string.Empty;
    }
}
