using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidateStatus
{
  public record UpdateCandidateStatusCommand(string Id, string Status) : IRequest;
}
