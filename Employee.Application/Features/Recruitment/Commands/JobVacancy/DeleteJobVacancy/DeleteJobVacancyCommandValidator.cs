using FluentValidation;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.DeleteJobVacancy
{
  public class DeleteJobVacancyCommandValidator : AbstractValidator<DeleteJobVacancyCommand>
  {
    public DeleteJobVacancyCommandValidator()
    {
      RuleFor(x => x.Id)
          .NotEmpty().WithMessage("Job vacancy ID is required.");
    }
  }
}
