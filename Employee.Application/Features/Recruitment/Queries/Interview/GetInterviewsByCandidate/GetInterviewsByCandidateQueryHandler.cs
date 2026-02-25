using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Application.Features.Recruitment.Queries.Interview.GetInterviewsByCandidate
{
  public class GetInterviewsByCandidateQueryHandler : IRequestHandler<GetInterviewsByCandidateQuery, IEnumerable<InterviewResponseDto>>
  {
    private readonly IInterviewRepository _repo;

    public GetInterviewsByCandidateQueryHandler(IInterviewRepository repo)
    {
      _repo = repo;
    }

    public async Task<IEnumerable<InterviewResponseDto>> Handle(GetInterviewsByCandidateQuery request, CancellationToken cancellationToken)
    {
      var entities = await _repo.GetByCandidateIdAsync(request.CandidateId, cancellationToken);
      return entities.Select(e => e.ToDto());
    }
  }
}
