using MediatR;

namespace Employee.Application.Features.Auth.Commands.ToggleUserStatus
{
    public class ToggleUserStatusCommand : IRequest
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
