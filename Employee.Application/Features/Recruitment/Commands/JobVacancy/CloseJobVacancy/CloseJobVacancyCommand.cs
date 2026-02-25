using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CloseJobVacancy
{
  public record CloseJobVacancyCommand(string Id) : IRequest;
}
