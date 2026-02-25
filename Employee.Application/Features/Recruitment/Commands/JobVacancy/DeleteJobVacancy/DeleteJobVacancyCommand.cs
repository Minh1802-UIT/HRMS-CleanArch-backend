using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.DeleteJobVacancy
{
  public record DeleteJobVacancyCommand(string Id) : IRequest;
}
