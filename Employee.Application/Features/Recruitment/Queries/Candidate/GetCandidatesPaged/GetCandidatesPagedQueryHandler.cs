using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidatesPaged;

public class GetCandidatesPagedQueryHandler : IRequestHandler<GetCandidatesPagedQuery, PagedResult<CandidateResponseDto>>
{
    private readonly ICandidateRepository _repo;

    public GetCandidatesPagedQueryHandler(ICandidateRepository repo)
    {
        _repo = repo;
    }

    public async Task<PagedResult<CandidateResponseDto>> Handle(GetCandidatesPagedQuery request, CancellationToken cancellationToken)
    {
      var pagination = request.Pagination;
      if (string.IsNullOrEmpty(pagination.SortBy))
        pagination.SortBy = "AppliedDate";
      var pagedCandidates = await _repo.GetPagedAsync(pagination, cancellationToken);

        return new PagedResult<CandidateResponseDto>
        {
            Items = pagedCandidates.Items.Select(c => c.ToDto()).ToList(),
            TotalCount = pagedCandidates.TotalCount,
            PageNumber = pagedCandidates.PageNumber,
            PageSize = pagedCandidates.PageSize
        };
    }
}
