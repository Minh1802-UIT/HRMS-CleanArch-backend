using FluentValidation;

namespace Employee.Application.Features.Attendance.Commands.CheckIn
{
  public class CheckInCommandValidator : AbstractValidator<CheckInCommand>
  {
    public CheckInCommandValidator()
    {
      RuleFor(x => x.EmployeeId)
          .NotEmpty().WithMessage("Employee ID is required.");

      RuleFor(x => x.Dto.Type)
          .NotEmpty().WithMessage("Type is required.")
          .Must(t => t == "CheckIn" || t == "CheckOut")
          .WithMessage("Type must be 'CheckIn' or 'CheckOut'.");
    }
  }
}
