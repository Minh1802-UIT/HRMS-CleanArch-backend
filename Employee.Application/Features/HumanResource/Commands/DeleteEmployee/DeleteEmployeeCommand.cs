using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.HumanResource.Commands.DeleteEmployee
{
    [Authorize(Roles = "Admin")]
public class DeleteEmployeeCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;

        public DeleteEmployeeCommand(string id)
        {
            Id = id;
        }
    }
}
