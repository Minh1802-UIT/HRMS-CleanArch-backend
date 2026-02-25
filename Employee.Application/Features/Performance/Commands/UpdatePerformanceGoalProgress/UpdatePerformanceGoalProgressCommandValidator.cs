using FluentValidation;

namespace Employee.Application.Features.Performance.Commands.UpdatePerformanceGoalProgress
{
  public class UpdatePerformanceGoalProgressCommandValidator : AbstractValidator<UpdatePerformanceGoalProgressCommand>
  {
    public UpdatePerformanceGoalProgressCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Goal ID is required.");

      RuleFor(x => x.Progress)
          .InclusiveBetween(0.0, 100.0).WithMessage("Progress must be between 0 and 100.");
    }
  }
}
