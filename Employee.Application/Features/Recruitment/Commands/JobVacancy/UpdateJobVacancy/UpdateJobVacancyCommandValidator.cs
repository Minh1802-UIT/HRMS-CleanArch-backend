using FluentValidation;
using Employee.Application.Features.Recruitment.Validators;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.UpdateJobVacancy
{
  public class UpdateJobVacancyCommandValidator : AbstractValidator<UpdateJobVacancyCommand>
  {
    public UpdateJobVacancyCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Job vacancy ID is required.");

      RuleFor(x => x.Dto)
          .NotNull().WithMessage("Job vacancy data is required.")
          .SetValidator(new JobVacancyValidator());
    }
  }
}
