using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidatesByVacancy
{
  public class GetCandidatesByVacancyQueryHandler : IRequestHandler<GetCandidatesByVacancyQuery, IEnumerable<CandidateResponseDto>>
  {
    private readonly ICandidateRepository _repo;

    public GetCandidatesByVacancyQueryHandler(ICandidateRepository repo)
    {
      _repo = repo;
    }

    public async Task<IEnumerable<CandidateResponseDto>> Handle(GetCandidatesByVacancyQuery request, CancellationToken cancellationToken)
    {
      var entities = await _repo.GetByVacancyIdAsync(request.VacancyId, cancellationToken);
      return entities.Select(e => e.ToDto());
    }
  }
}
