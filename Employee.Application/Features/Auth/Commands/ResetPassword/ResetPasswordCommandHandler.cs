using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.ResetPassword
{
  public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
  {
    private readonly IIdentityService _identityService;

    public ResetPasswordCommandHandler(IIdentityService identityService)
    {
      _identityService = identityService;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
      var result = await _identityService.ResetPasswordAsync(request.Email.Trim(), request.Token, request.NewPassword);

      if (!result.Succeeded)
      {
        var errors = string.Join(", ", result.Errors);
        throw new ValidationException($"Đặt lại mật khẩu thất bại: {errors}");
      }
    }
  }
}
