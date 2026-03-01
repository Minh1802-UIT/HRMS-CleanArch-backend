using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.DeleteUser
{
    [Authorize(Roles = "Admin")]
    public class DeleteUserByEmployeeIdCommand : IRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
    }
}
