using Employee.Domain.Common.Models;
using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.GetVacanciesPaged;

public record GetVacanciesPagedQuery(PaginationParams Pagination) : IRequest<PagedResult<JobVacancyResponseDto>>;
