using FluentValidation;

namespace Employee.Application.Features.Organization.Commands.UpdatePosition
{
  public class UpdatePositionCommandValidator : AbstractValidator<UpdatePositionCommand>
  {
    public UpdatePositionCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Position ID is required.");

      RuleFor(x => x.Dto.Title)
          .NotEmpty().WithMessage("Position title is required.")
          .MaximumLength(100).WithMessage("Position title must not exceed 100 characters.");

      RuleFor(x => x.Dto.SalaryRange!.Min)
          .GreaterThanOrEqualTo(0).WithMessage("Minimum salary must be non-negative.")
          .When(x => x.Dto.SalaryRange != null);

      RuleFor(x => x.Dto.SalaryRange!.Max)
          .GreaterThanOrEqualTo(x => x.Dto.SalaryRange!.Min).WithMessage("Maximum salary must be greater than or equal to minimum salary.")
          .When(x => x.Dto.SalaryRange != null);
    }
  }
}
