using FluentValidation;

namespace Employee.Application.Features.Organization.Commands.UpdateDepartment
{
  public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
  {
    public UpdateDepartmentCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Department ID is required.");

      RuleFor(x => x.Dto.Name)
          .NotEmpty().WithMessage("Department name is required.")
          .MaximumLength(100).WithMessage("Department name must not exceed 100 characters.");

      RuleFor(x => x.Dto.Description)
          .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
          .When(x => x.Dto.Description != null);
    }
  }
}
