using FluentValidation;

namespace Employee.Application.Features.Auth.Commands.RefreshToken
{
  public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
  {
    public RefreshTokenCommandValidator()
    {
      // AccessToken is optional: on page-reload the frontend has no in-memory token
      // and sends an empty string. The backend locates the user via the refresh
      // token hash in that case. When present, the token is validated as before.

      RuleFor(x => x.RefreshToken)
          .NotEmpty().WithMessage("Refresh token is required.");
    }
  }
}
