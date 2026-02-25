using FluentValidation;

namespace Employee.Application.Features.Auth.Commands.Login
{
  /// <summary>
  /// FluentValidation validator for LoginCommand.
  /// Ensures username and password are present before the handler is invoked,
  /// consistent with every other command in the CQRS pipeline.
  /// </summary>
  public class LoginCommandValidator : AbstractValidator<LoginCommand>
  {
    public LoginCommandValidator()
    {
      RuleFor(x => x.Username)
          .NotEmpty().WithMessage("Username is required.")
          .MaximumLength(100).WithMessage("Username must not exceed 100 characters.");

      RuleFor(x => x.Password)
          .NotEmpty().WithMessage("Password is required.")
          .MinimumLength(6).WithMessage("Password must be at least 6 characters.");
    }
  }
}
