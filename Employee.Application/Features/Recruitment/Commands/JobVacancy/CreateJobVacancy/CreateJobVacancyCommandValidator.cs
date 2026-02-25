using FluentValidation;
using Employee.Application.Features.Recruitment.Validators;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CreateJobVacancy
{
  public class CreateJobVacancyCommandValidator : AbstractValidator<CreateJobVacancyCommand>
  {
    public CreateJobVacancyCommandValidator()
    {
      RuleFor(x => x.Dto)
          .NotNull().WithMessage("Job vacancy data is required.")
          .SetValidator(new JobVacancyValidator());
    }
  }
}
