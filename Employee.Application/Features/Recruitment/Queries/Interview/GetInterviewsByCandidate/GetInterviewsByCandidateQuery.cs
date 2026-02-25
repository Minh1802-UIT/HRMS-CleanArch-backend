using Employee.Application.Features.Recruitment.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Recruitment.Queries.Interview.GetInterviewsByCandidate
{
  public record GetInterviewsByCandidateQuery(string CandidateId) : IRequest<IEnumerable<InterviewResponseDto>>;
}
