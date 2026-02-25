using FluentValidation;

namespace Employee.Application.Features.Organization.Commands.CreateDepartment
{
  public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
  {
    public CreateDepartmentCommandValidator()
    {
      RuleFor(x => x.Dto.Code)
          .NotEmpty().WithMessage("Department code is required.")
          .MaximumLength(20).WithMessage("Department code must not exceed 20 characters.")
          .Matches(@"^[a-zA-Z0-9-_]+$").WithMessage("Department code can only contain letters, numbers, hyphens, and underscores.");

      RuleFor(x => x.Dto.Name)
          .NotEmpty().WithMessage("Department name is required.")
          .MaximumLength(100).WithMessage("Department name must not exceed 100 characters.");

      RuleFor(x => x.Dto.Description)
          .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
          .When(x => x.Dto.Description != null);
    }
  }
}
