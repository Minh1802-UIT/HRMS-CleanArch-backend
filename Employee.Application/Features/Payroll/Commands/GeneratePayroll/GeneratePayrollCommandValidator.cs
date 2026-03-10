using FluentValidation;

namespace Employee.Application.Features.Payroll.Commands.GeneratePayroll
{
  public class GeneratePayrollCommandValidator : AbstractValidator<GeneratePayrollCommand>
  {
    public GeneratePayrollCommandValidator()
    {
      RuleFor(x => x.Month)
          .NotEmpty().WithMessage("Month is required.")
          .Matches(@"^\d{2}-\d{4}$").WithMessage("Month must be in format MM-yyyy (e.g., 01-2025).")
          .Must(BeAValidDatePeriod).WithMessage("The specified month and year combination is mathematically invalid or out of bounds (1-12 months, year > 2000).");
    }

    private bool BeAValidDatePeriod(string month)
    {
      if (string.IsNullOrEmpty(month)) return false;
      var parts = month.Split('-');
      if (parts.Length != 2) return false;
      if (!int.TryParse(parts[0], out var m) || !int.TryParse(parts[1], out var y)) return false;
      return m >= 1 && m <= 12 && y >= 2000 && y <= 2100;
    }
  }
}
