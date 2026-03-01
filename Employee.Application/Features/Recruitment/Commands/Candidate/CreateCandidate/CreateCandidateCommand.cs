using Employee.Application.Common.Security;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.CreateCandidate
{
  [Authorize(Roles = "Admin,HR")]
public record CreateCandidateCommand(CandidateDto Dto) : IRequest;
}
