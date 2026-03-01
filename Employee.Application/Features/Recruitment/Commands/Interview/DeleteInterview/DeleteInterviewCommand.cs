using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Interview.DeleteInterview
{
  [Authorize(Roles = "Admin,HR")]
public record DeleteInterviewCommand(string Id) : IRequest;
}
