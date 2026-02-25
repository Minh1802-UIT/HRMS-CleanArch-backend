using FluentValidation;

namespace Employee.Application.Features.Recruitment.Commands.Interview.ReviewInterview
{
  public class ReviewInterviewCommandValidator : AbstractValidator<ReviewInterviewCommand>
  {
    private static readonly string[] ValidResults = { "Completed", "Cancelled" };

    public ReviewInterviewCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Interview ID is required.");

      RuleFor(x => x.Dto.Result)
          .NotEmpty().WithMessage("Result is required.")
          .Must(r => ValidResults.Contains(r))
          .WithMessage("Result must be 'Completed' or 'Cancelled'.");
    }
  }
}
