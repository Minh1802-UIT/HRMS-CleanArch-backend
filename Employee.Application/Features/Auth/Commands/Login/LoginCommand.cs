using MediatR;
using Employee.Application.Features.Auth.Dtos;

namespace Employee.Application.Features.Auth.Commands.Login
{
    public class LoginCommand : IRequest<LoginResponseDto>
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
