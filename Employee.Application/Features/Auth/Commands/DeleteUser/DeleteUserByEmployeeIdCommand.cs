using MediatR;

namespace Employee.Application.Features.Auth.Commands.DeleteUser
{
    public class DeleteUserByEmployeeIdCommand : IRequest
    {
        public string EmployeeId { get; set; } = string.Empty;
    }
}
