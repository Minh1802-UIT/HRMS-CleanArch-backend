using FluentValidation;

namespace Employee.Application.Features.Payroll.Commands.MarkPayrollPaid
{
  public class UpdatePayrollStatusCommandValidator : AbstractValidator<UpdatePayrollStatusCommand>
  {
    private static readonly string[] ValidStatuses = { "Draft", "Approved", "Paid", "Rejected" };

    public UpdatePayrollStatusCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Payroll ID is required.");

      RuleFor(x => x.Status)
          .NotEmpty().WithMessage("Status is required.")
          .Must(s => ValidStatuses.Contains(s))
          .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
  }
}
