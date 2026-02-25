using FluentValidation;

namespace Employee.Application.Features.Leave.Commands.CancelLeaveRequest
{
  public class CancelLeaveRequestCommandValidator : AbstractValidator<CancelLeaveRequestCommand>
  {
    public CancelLeaveRequestCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Leave request ID is required.");

      RuleFor(x => x.EmployeeId)
          .NotEmpty().WithMessage("Employee ID is required.");
    }
  }
}
