using FluentValidation;
using Employee.Application.Features.Recruitment.Validators;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidate
{
  public class UpdateCandidateCommandValidator : AbstractValidator<UpdateCandidateCommand>
  {
    public UpdateCandidateCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Candidate ID is required.");

      RuleFor(x => x.Dto)
          .NotNull().WithMessage("Candidate data is required.")
          .SetValidator(new CandidateValidator());
    }
  }
}
