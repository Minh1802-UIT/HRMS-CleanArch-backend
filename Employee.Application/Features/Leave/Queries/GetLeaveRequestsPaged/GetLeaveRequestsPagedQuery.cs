using Employee.Domain.Common.Models;
using Employee.Application.Features.Leave.Dtos;
using MediatR;

namespace Employee.Application.Features.Leave.Queries.GetLeaveRequestsPaged
{
  public record GetLeaveRequestsPagedQuery(PaginationParams Pagination) : IRequest<PagedResult<LeaveRequestListDto>>;
}
