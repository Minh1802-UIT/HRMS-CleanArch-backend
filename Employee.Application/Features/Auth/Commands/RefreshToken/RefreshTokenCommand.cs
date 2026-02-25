using Employee.Application.Features.Auth.Dtos;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.RefreshToken
{
  public class RefreshTokenCommand : IRequest<LoginResponseDto>
  {
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
  }
}
