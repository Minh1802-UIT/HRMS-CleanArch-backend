using FluentValidation;
using Employee.Domain.Enums;

namespace Employee.Application.Features.Leave.Commands.UpdateLeaveRequest
{
  public class UpdateLeaveRequestCommandValidator : AbstractValidator<UpdateLeaveRequestCommand>
  {
    public UpdateLeaveRequestCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Leave request ID is required.");

      RuleFor(x => x.EmployeeId)
          .NotEmpty().WithMessage("Employee ID is required.");

      RuleFor(x => x.Dto.LeaveType)
          .NotEmpty().WithMessage("Leave type is required.")
          .IsEnumName(typeof(LeaveTypeEnum), caseSensitive: false)
          .WithMessage("Leave type must be a valid value (Annual, Sick, Unpaid).");

      RuleFor(x => x.Dto.FromDate)
          .NotEmpty().WithMessage("From date is required.");

      RuleFor(x => x.Dto.ToDate)
          .NotEmpty().WithMessage("To date is required.")
          .GreaterThanOrEqualTo(x => x.Dto.FromDate).WithMessage("End date must be after start date.");

      RuleFor(x => x.Dto.Reason)
          .NotEmpty().WithMessage("Reason is required.")
          .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
  }
}
