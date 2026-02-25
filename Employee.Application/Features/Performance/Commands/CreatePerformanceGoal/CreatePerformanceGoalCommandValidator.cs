using FluentValidation;
using System;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceGoal
{
  public class CreatePerformanceGoalCommandValidator : AbstractValidator<CreatePerformanceGoalCommand>
  {
    public CreatePerformanceGoalCommandValidator()
    {
      RuleFor(x => x.Dto.EmployeeId)
          .NotEmpty().WithMessage("Employee ID is required.");

      RuleFor(x => x.Dto.Title)
          .NotEmpty().WithMessage("Goal title is required.")
          .MaximumLength(200).WithMessage("Goal title must not exceed 200 characters.");

      RuleFor(x => x.Dto.TargetDate)
          .NotEmpty().WithMessage("Target date is required.")
          .GreaterThan(DateTime.UtcNow).WithMessage("Target date must be in the future.");
    }
  }
}
