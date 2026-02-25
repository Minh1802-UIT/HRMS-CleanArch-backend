using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidate
{
  public record UpdateCandidateCommand(string Id, CandidateDto Dto) : IRequest;
}
