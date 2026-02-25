using FluentValidation;

namespace Employee.Application.Features.Auth.Commands.ResetPassword
{
  public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
  {
    public ResetPasswordCommandValidator()
    {
      RuleFor(x => x.Email)
          .NotEmpty().WithMessage("Email is required.")
          .EmailAddress().WithMessage("Invalid email format.");

      RuleFor(x => x.Token)
          .NotEmpty().WithMessage("Reset token is required.");

      RuleFor(x => x.NewPassword)
          .NotEmpty().WithMessage("New password is required.")
          .MinimumLength(6).WithMessage("New password must be at least 6 characters.")
          .MaximumLength(100).WithMessage("New password must not exceed 100 characters.");
    }
  }
}
