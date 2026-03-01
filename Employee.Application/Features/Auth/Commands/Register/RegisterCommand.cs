using Employee.Application.Common.Security;
using MediatR;
using Employee.Application.Features.Auth.Dtos;

namespace Employee.Application.Features.Auth.Commands.Register
{
    [Authorize(Roles = "Admin")]
public class RegisterCommand : IRequest<string>
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        /// <summary>When true the user is forced to change their password at first login.</summary>
        public bool MustChangePassword { get; set; } = false;
    }
}
