using FluentValidation;

namespace Employee.Application.Features.Organization.Commands.DeleteDepartment
{
  public class DeleteDepartmentCommandValidator : AbstractValidator<DeleteDepartmentCommand>
  {
    public DeleteDepartmentCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Department ID is required.");
    }
  }
}
