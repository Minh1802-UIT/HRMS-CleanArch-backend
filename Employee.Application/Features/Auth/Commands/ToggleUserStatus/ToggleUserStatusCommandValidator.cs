using FluentValidation;

namespace Employee.Application.Features.Auth.Commands.ToggleUserStatus
{
  public class ToggleUserStatusCommandValidator : AbstractValidator<ToggleUserStatusCommand>
  {
    public ToggleUserStatusCommandValidator()
    {
      RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required.");
    }
  }
}
