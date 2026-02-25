using FluentValidation;
using Employee.Application.Features.Recruitment.Validators;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.CreateCandidate
{
  public class CreateCandidateCommandValidator : AbstractValidator<CreateCandidateCommand>
  {
    public CreateCandidateCommandValidator()
    {
      RuleFor(x => x.Dto)
          .NotNull().WithMessage("Candidate data is required.")
          .SetValidator(new CandidateValidator());
    }
  }
}
