using FluentValidation;

namespace Employee.Application.Features.HumanResource.Commands.DeleteEmployee
{
  public class DeleteEmployeeCommandValidator : AbstractValidator<DeleteEmployeeCommand>
  {
    public DeleteEmployeeCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Employee ID is required.");
    }
  }
}
