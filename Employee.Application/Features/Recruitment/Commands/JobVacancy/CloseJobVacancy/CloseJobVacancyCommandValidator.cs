using FluentValidation;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CloseJobVacancy
{
  public class CloseJobVacancyCommandValidator : AbstractValidator<CloseJobVacancyCommand>
  {
    public CloseJobVacancyCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Job vacancy ID is required.");
    }
  }
}
