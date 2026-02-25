using Employee.Application.Common.Models;
using Employee.Application.Common.Interfaces;
using Employee.Application.Features.Auth.Dtos;
using MediatR;

namespace Employee.Application.Features.Auth.Queries.GetUsers
{
  public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
  {
    private readonly IIdentityService _identityService;

    public GetUsersQueryHandler(IIdentityService identityService)
    {
      _identityService = identityService;
    }

    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
      return await _identityService.GetPagedUsersAsync(request.Pagination);
    }
  }
}
