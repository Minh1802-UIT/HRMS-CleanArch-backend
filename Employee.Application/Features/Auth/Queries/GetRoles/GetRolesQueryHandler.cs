using Employee.Application.Common.Interfaces;
using MediatR;

namespace Employee.Application.Features.Auth.Queries.GetRoles
{
  public class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IEnumerable<string>>
  {
    private readonly IIdentityService _identityService;

    public GetRolesQueryHandler(IIdentityService identityService)
    {
      _identityService = identityService;
    }

    public async Task<IEnumerable<string>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
      return await _identityService.GetRolesAsync();
    }
  }
}
