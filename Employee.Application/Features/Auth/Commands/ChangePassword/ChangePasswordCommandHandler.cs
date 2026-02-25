using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.ChangePassword
{
    public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
    {
    private readonly IIdentityService _identityService;

    public ChangePasswordCommandHandler(IIdentityService identityService)
        {
      _identityService = identityService;
        }

        public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
        {
      var result = await _identityService.ChangePasswordAsync(request.UserId, request.CurrentPassword, request.NewPassword);

      if (!result.Succeeded)
      {
        throw new ValidationException(string.Join(", ", result.Errors));
      }
        }
    }
}
