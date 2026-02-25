using FluentValidation;

namespace Employee.Application.Features.Organization.Commands.CreatePosition
{
  public class CreatePositionCommandValidator : AbstractValidator<CreatePositionCommand>
  {
    public CreatePositionCommandValidator()
    {
      RuleFor(x => x.Dto.Title)
          .NotEmpty().WithMessage("Position title is required.")
          .MaximumLength(100).WithMessage("Position title must not exceed 100 characters.");

      RuleFor(x => x.Dto.Code)
          .NotEmpty().WithMessage("Position code is required.")
          .MaximumLength(20).WithMessage("Position code must not exceed 20 characters.")
          .Matches(@"^[A-Z0-9-_]+$").WithMessage("Position code can only contain uppercase letters, numbers, hyphens, and underscores.");

      RuleFor(x => x.Dto.DepartmentId)
          .NotEmpty().WithMessage("Department ID is required.");

      RuleFor(x => x.Dto.SalaryRange!.Min)
          .GreaterThanOrEqualTo(0).WithMessage("Minimum salary must be non-negative.")
          .When(x => x.Dto.SalaryRange != null);

      RuleFor(x => x.Dto.SalaryRange!.Max)
          .GreaterThanOrEqualTo(x => x.Dto.SalaryRange!.Min).WithMessage("Maximum salary must be greater than or equal to minimum salary.")
          .When(x => x.Dto.SalaryRange != null);
    }
  }
}
