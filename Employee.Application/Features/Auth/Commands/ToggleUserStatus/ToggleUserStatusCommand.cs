using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.ToggleUserStatus
{
    [Authorize(Roles = "Admin,HR")]
public class ToggleUserStatusCommand : IRequest
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
