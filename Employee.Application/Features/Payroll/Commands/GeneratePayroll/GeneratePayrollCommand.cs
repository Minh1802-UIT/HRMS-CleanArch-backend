using Employee.Application.Features.Payroll.Dtos;
using MediatR;

namespace Employee.Application.Features.Payroll.Commands.GeneratePayroll
{
    public class GeneratePayrollCommand : IRequest<int>
    {
        public string Month { get; set; } = string.Empty; // Format: "MM-yyyy"
        
        // Option specific employees
        public List<string>? EmployeeIds { get; set; }
    }
}
