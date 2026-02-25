using FluentValidation;
using Employee.Application.Features.Recruitment.Validators;

namespace Employee.Application.Features.Recruitment.Commands.Interview.CreateInterview
{
  public class CreateInterviewCommandValidator : AbstractValidator<CreateInterviewCommand>
  {
    public CreateInterviewCommandValidator()
    {
      RuleFor(x => x.Dto)
          .NotNull().WithMessage("Interview data is required.")
          .SetValidator(new InterviewValidator());
    }
  }
}
