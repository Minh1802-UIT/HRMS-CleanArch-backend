using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Dtos;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.RefreshToken
{
  public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResponseDto>
  {
    private readonly IIdentityService _identityService;

    public RefreshTokenCommandHandler(IIdentityService identityService)
    {
      _identityService = identityService;
    }

    public async Task<LoginResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
      return await _identityService.RefreshTokenAsync(request.AccessToken, request.RefreshToken);
    }
  }
}
