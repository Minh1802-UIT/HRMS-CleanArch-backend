using FluentValidation;

namespace Employee.Application.Features.Recruitment.Commands.Interview.DeleteInterview
{
  public class DeleteInterviewCommandValidator : AbstractValidator<DeleteInterviewCommand>
  {
    public DeleteInterviewCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Interview ID is required.");
    }
  }
}
