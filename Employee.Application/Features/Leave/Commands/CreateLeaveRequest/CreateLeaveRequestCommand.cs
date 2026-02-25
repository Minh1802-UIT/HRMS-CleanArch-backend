using Employee.Application.Features.Leave.Dtos;
using MediatR;

namespace Employee.Application.Features.Leave.Commands.CreateLeaveRequest
{
    public class CreateLeaveRequestCommand : IRequest<LeaveRequestDto>
    {
        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; } = string.Empty;

        // EmployeeId will be populated from Token in the Handler or via property
        public string EmployeeId { get; set; } = string.Empty; 
    }
}
