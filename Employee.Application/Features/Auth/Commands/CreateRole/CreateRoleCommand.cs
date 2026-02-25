using MediatR;

namespace Employee.Application.Features.Auth.Commands.CreateRole
{
    public class CreateRoleCommand : IRequest<string>
    {
        public string RoleName { get; set; } = string.Empty;
    }
}
