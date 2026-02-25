using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidateById
{
  public record GetCandidateByIdQuery(string Id) : IRequest<CandidateResponseDto>;
}
