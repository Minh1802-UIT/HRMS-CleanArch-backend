using MediatR;

namespace Employee.Application.Features.Payroll.Commands.MarkPayrollPaid
{
    public class UpdatePayrollStatusCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
