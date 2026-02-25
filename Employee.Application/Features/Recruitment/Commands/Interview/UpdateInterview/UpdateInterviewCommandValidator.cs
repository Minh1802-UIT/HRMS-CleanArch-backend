using FluentValidation;
using Employee.Application.Features.Recruitment.Validators;

namespace Employee.Application.Features.Recruitment.Commands.Interview.UpdateInterview
{
  public class UpdateInterviewCommandValidator : AbstractValidator<UpdateInterviewCommand>
  {
    public UpdateInterviewCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Interview ID is required.");

      RuleFor(x => x.Dto)
          .NotNull().WithMessage("Interview data is required.")
          .SetValidator(new InterviewValidator());
    }
  }
}
