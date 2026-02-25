using Employee.Application.Features.Attendance.Dtos;
using MediatR;

namespace Employee.Application.Features.Attendance.Commands.CheckIn
{
    public class CheckInCommand : IRequest
    {
        public CheckInRequestDto Dto { get; set; } = new();
        public string EmployeeId { get; set; } = string.Empty; // Token
    }
}
