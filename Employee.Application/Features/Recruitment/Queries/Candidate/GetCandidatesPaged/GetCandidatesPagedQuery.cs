using Employee.Domain.Common.Models;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidatesPaged;

public record GetCandidatesPagedQuery(PaginationParams Pagination) : IRequest<PagedResult<CandidateResponseDto>>;
