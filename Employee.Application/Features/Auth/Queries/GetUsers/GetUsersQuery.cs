using Employee.Application.Common.Models;
using Employee.Application.Features.Auth.Dtos;
using MediatR;

namespace Employee.Application.Features.Auth.Queries.GetUsers
{
  public class GetUsersQuery : IRequest<PagedResult<UserDto>>
  {
    public PaginationParams Pagination { get; set; } = new();
  }
}
