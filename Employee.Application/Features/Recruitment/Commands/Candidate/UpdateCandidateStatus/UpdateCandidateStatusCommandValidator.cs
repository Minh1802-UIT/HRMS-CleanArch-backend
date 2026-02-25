using FluentValidation;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidateStatus
{
  public class UpdateCandidateStatusCommandValidator : AbstractValidator<UpdateCandidateStatusCommand>
  {
    private static readonly string[] ValidStatuses = { "Applied", "Interviewing", "Test", "Hired", "Rejected" };

    public UpdateCandidateStatusCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Candidate ID is required.");

      RuleFor(x => x.Status)
          .NotEmpty().WithMessage("Status is required.")
          .Must(s => ValidStatuses.Contains(s))
          .WithMessage($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
    }
  }
}
