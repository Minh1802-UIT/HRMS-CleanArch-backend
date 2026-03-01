using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Payroll.Commands.MarkPayrollPaid
{
    [Authorize(Roles = "Admin,HR")]
public class UpdatePayrollStatusCommand : IRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
