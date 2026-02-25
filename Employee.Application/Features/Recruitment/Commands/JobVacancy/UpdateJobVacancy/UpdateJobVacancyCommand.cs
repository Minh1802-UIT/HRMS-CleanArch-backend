using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.UpdateJobVacancy
{
  public record UpdateJobVacancyCommand(string Id, JobVacancyDto Dto) : IRequest;
}
