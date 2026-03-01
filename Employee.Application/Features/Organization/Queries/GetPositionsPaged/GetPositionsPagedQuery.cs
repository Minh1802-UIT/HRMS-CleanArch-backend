using Employee.Domain.Common.Models;
using Employee.Application.Features.Organization.Dtos;
using MediatR;

namespace Employee.Application.Features.Organization.Queries.GetPositionsPaged
{
  public record GetPositionsPagedQuery(PaginationParams Pagination) : IRequest<PagedResult<PositionDto>>;
}
