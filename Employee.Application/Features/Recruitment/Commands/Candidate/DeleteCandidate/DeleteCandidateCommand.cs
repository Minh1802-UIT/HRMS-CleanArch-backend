using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.DeleteCandidate
{
  [Authorize(Roles = "Admin,HR")]
public record DeleteCandidateCommand(string Id) : IRequest;
}
