using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.CreateCandidate
{
  public record CreateCandidateCommand(CandidateDto Dto) : IRequest;
}
