using Employee.Application.Features.Auth.Dtos;
using Employee.Application.Common.Interfaces;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.Login
{
  public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
  {
    private readonly IIdentityService _identityService;

    public LoginCommandHandler(IIdentityService identityService)
    {
      _identityService = identityService;
    }

    public async Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
      return await _identityService.LoginAsync(request.Username, request.Password);
    }
  }
}
