using FluentValidation;

namespace Employee.Application.Features.Organization.Commands.DeletePosition
{
  public class DeletePositionCommandValidator : AbstractValidator<DeletePositionCommand>
  {
    public DeletePositionCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Position ID is required.");
    }
  }
}
