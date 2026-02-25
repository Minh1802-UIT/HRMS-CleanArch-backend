using Employee.Application.Features.Auth.Dtos;
using Employee.Application.Common.Interfaces;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.Register
{
  public class RegisterCommandHandler : IRequestHandler<RegisterCommand, string>
  {
    private readonly IIdentityService _identityService;

    public RegisterCommandHandler(IIdentityService identityService)
    {
      _identityService = identityService;
    }

    public async Task<string> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
      var dto = new RegisterDto
      {
        Username = request.Username,
        Email = request.Email,
        Password = request.Password,
        FullName = request.FullName,
        EmployeeId = request.EmployeeId,
        MustChangePassword = request.MustChangePassword
      };

      return await _identityService.RegisterAsync(dto);
    }
  }
}
