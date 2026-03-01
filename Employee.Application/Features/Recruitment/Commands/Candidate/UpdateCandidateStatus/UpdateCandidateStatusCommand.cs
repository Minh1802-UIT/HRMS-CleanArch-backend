using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidateStatus
{
  [Authorize(Roles = "Admin,HR")]
public record UpdateCandidateStatusCommand(string Id, string Status) : IRequest;
}
