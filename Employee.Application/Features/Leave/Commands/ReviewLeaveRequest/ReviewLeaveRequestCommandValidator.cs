using FluentValidation;

namespace Employee.Application.Features.Leave.Commands.ReviewLeaveRequest
{
  public class ReviewLeaveRequestCommandValidator : AbstractValidator<ReviewLeaveRequestCommand>
  {
    public ReviewLeaveRequestCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Leave request ID is required.");

      RuleFor(x => x.ReviewDto.Status)
          .NotEmpty().WithMessage("Status is required.")
          .Must(s => s == "Approved" || s == "Rejected")
          .WithMessage("Status must be 'Approved' or 'Rejected'.");

      RuleFor(x => x.ReviewDto.ManagerComment)
          .MaximumLength(500).WithMessage("Manager comment must not exceed 500 characters.");
    }
  }
}
