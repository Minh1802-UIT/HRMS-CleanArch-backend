using FluentValidation;

namespace Employee.Application.Features.Payroll.Commands.GeneratePayroll
{
  public class GeneratePayrollCommandValidator : AbstractValidator<GeneratePayrollCommand>
  {
    public GeneratePayrollCommandValidator()
    {
      RuleFor(x => x.Month)
          .NotEmpty().WithMessage("Month is required.")
          .Matches(@"^\d{2}-\d{4}$").WithMessage("Month must be in format MM-yyyy (e.g., 01-2025).");
    }
  }
}
