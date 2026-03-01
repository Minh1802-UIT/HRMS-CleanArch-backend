using FluentValidation;
using Employee.Domain.Enums;

namespace Employee.Application.Features.Leave.Commands.CreateLeaveRequest
{
    public class CreateLeaveRequestValidator : AbstractValidator<CreateLeaveRequestCommand>
    {
        public CreateLeaveRequestValidator()
        {
            RuleFor(p => p.LeaveType)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .IsEnumName(typeof(LeaveCategory), caseSensitive: false)
                .WithMessage("{PropertyName} must be a valid leave type (Annual, Sick, Unpaid).");

            RuleFor(p => p.FromDate)
                .NotEmpty().WithMessage("{PropertyName} is required.");

            RuleFor(p => p.ToDate)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .GreaterThanOrEqualTo(p => p.FromDate).WithMessage("End Date must be after Start Date");

            RuleFor(p => p.Reason)
                .NotEmpty().WithMessage("{PropertyName} is required.")
                .MaximumLength(300).WithMessage("{PropertyName} must not exceed 300 characters.");
        }
    }
}
