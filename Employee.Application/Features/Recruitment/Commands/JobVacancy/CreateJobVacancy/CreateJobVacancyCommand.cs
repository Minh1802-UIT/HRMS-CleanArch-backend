using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CreateJobVacancy
{
  public record CreateJobVacancyCommand(JobVacancyDto Dto) : IRequest;
}
