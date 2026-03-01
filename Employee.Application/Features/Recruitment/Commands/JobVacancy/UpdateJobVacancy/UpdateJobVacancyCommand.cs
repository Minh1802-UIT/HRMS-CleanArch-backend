using Employee.Application.Common.Security;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.UpdateJobVacancy
{
  [Authorize(Roles = "Admin,HR")]
public record UpdateJobVacancyCommand(string Id, JobVacancyDto Dto) : IRequest;
}
