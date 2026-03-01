using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Application.Features.Recruitment.Queries.GetAllJobVacancies
{
  public class GetAllJobVacanciesQueryHandler : IRequestHandler<GetAllJobVacanciesQuery, IEnumerable<JobVacancyResponseDto>>
  {
    private readonly IJobVacancyRepository _repo;

    public GetAllJobVacanciesQueryHandler(IJobVacancyRepository repo)
    {
      _repo = repo;
    }

    public async Task<IEnumerable<JobVacancyResponseDto>> Handle(GetAllJobVacanciesQuery request, CancellationToken cancellationToken)
    {
      var entities = await _repo.GetAllAsync(cancellationToken);
      return entities.Select(e => e.ToDto());
    }
  }
}
