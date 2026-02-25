using MediatR;

namespace Employee.Application.Features.Auth.Queries.GetRoles
{
    public class GetRolesQuery : IRequest<IEnumerable<string>>
    {
    }
}
