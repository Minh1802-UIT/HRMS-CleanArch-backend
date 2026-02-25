using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.DeleteCandidate
{
  public record DeleteCandidateCommand(string Id) : IRequest;
}
