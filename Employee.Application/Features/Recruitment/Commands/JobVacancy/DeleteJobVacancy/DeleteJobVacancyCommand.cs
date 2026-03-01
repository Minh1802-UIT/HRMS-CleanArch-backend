using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.DeleteJobVacancy
{
  [Authorize(Roles = "Admin,HR")]
public record DeleteJobVacancyCommand(string Id) : IRequest;
}
