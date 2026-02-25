using FluentValidation;

namespace Employee.Application.Features.Auth.Commands.AssignRole
{
  public class AssignRoleCommandValidator : AbstractValidator<AssignRoleCommand>
  {
    public AssignRoleCommandValidator()
    {
      RuleFor(x => x.Username)
          .NotEmpty().WithMessage("Username is required.")
          .MaximumLength(100).WithMessage("Username must not exceed 100 characters.");

      RuleFor(x => x.RoleName)
          .NotEmpty().WithMessage("Role name is required.")
          .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");
    }
  }
}
