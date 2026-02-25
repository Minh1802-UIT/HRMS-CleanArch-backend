using Employee.Application.Features.Leave.Dtos;
using MediatR;

namespace Employee.Application.Features.Leave.Commands.UpdateLeaveRequest
{
    public class UpdateLeaveRequestCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public UpdateLeaveRequestDto Dto { get; set; } = new();
        public string EmployeeId { get; set; } = string.Empty;
    }
}
