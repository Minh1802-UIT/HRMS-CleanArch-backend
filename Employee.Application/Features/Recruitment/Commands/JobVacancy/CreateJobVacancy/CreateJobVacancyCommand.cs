using Employee.Application.Common.Security;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CreateJobVacancy
{
  [Authorize(Roles = "Admin,HR")]
public record CreateJobVacancyCommand(JobVacancyDto Dto) : IRequest;
}
