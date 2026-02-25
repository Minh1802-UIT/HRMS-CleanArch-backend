using MediatR;

namespace Employee.Application.Features.Leave.Commands.CancelLeaveRequest
{
    public class CancelLeaveRequestCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty; // From Token

        public CancelLeaveRequestCommand(string id, string employeeId)
        {
            Id = id;
            EmployeeId = employeeId;
        }
    }
}
