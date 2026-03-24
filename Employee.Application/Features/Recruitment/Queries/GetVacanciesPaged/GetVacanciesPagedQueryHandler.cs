using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.GetVacanciesPaged;

public class GetVacanciesPagedQueryHandler : IRequestHandler<GetVacanciesPagedQuery, PagedResult<JobVacancyResponseDto>>
{
    private readonly IJobVacancyRepository _repo;

    public GetVacanciesPagedQueryHandler(IJobVacancyRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<JobVacancyResponseDto>> Handle(GetVacanciesPagedQuery request, CancellationToken cancellationToken)
    {
      var pagination = request.Pagination;
      if (string.IsNullOrEmpty(pagination.SortBy))
        pagination.SortBy = "CreatedAt";
      var pagedVacancies = await _repo.GetPagedAsync(pagination, cancellationToken);

        return new PagedResult<JobVacancyResponseDto>
        {
            Items = pagedVacancies.Items.Select(v => v.ToDto()).ToList(),
            TotalCount = pagedVacancies.TotalCount,
            PageNumber = pagedVacancies.PageNumber,
            PageSize = pagedVacancies.PageSize
        };
    }
}
