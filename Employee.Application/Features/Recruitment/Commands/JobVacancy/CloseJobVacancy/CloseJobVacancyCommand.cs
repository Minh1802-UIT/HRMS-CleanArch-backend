using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CloseJobVacancy
{
  [Authorize(Roles = "Admin,HR")]
public record CloseJobVacancyCommand(string Id) : IRequest;
}
