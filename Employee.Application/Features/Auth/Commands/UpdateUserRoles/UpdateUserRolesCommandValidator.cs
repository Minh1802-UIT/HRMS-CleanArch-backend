using FluentValidation;

namespace Employee.Application.Features.Auth.Commands.UpdateUserRoles
{
  public class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
  {
    public UpdateUserRolesCommandValidator()
    {
      RuleFor(x => x.UserId)
          .NotEmpty().WithMessage("User ID is required.");

      RuleFor(x => x.RoleNames)
          .NotEmpty().WithMessage("At least one role must be provided.");

      RuleForEach(x => x.RoleNames)
          .NotEmpty().WithMessage("Role name must not be empty.");
    }
  }
}
