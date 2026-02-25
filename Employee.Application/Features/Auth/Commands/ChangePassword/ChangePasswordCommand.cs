using MediatR;

namespace Employee.Application.Features.Auth.Commands.ChangePassword
{
    public class ChangePasswordCommand : IRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
