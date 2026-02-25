using FluentValidation;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceReview
{
  public class CreatePerformanceReviewCommandValidator : AbstractValidator<CreatePerformanceReviewCommand>
  {
    public CreatePerformanceReviewCommandValidator()
    {
      RuleFor(x => x.Dto.EmployeeId)
          .NotEmpty().WithMessage("Employee ID is required.");

      RuleFor(x => x.Dto.ReviewerId)
          .NotEmpty().WithMessage("Reviewer ID is required.");

      RuleFor(x => x.Dto.PeriodStart)
          .NotEmpty().WithMessage("Period start date is required.");

      RuleFor(x => x.Dto.PeriodEnd)
          .NotEmpty().WithMessage("Period end date is required.")
          .GreaterThan(x => x.Dto.PeriodStart).WithMessage("Period end must be after period start.");

      RuleFor(x => x.Dto.OverallScore)
          .InclusiveBetween(0.0, 5.0).WithMessage("Overall score must be between 0 and 5.");
    }
  }
}
