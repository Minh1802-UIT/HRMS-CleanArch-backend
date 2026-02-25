using FluentValidation;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.DeleteCandidate
{
  public class DeleteCandidateCommandValidator : AbstractValidator<DeleteCandidateCommand>
  {
    public DeleteCandidateCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Candidate ID is required.");
    }
  }
}
