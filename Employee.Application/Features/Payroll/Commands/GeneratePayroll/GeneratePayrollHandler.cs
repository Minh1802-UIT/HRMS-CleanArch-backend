using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Exceptions;
using MediatR;
using Employee.Domain.Common.Models;

namespace Employee.Application.Features.Payroll.Commands.GeneratePayroll
{
  /// <summary>
  /// Orchestrates payroll generation: validates input, delegates calculation to
  /// PayrollProcessingService (shared with PayrollBackgroundService), and
  /// returns the count of generated payslips.
  /// </summary>
  public class GeneratePayrollHandler : IRequestHandler<GeneratePayrollCommand, int>
  {
    private readonly IPayrollProcessingService _payrollService;

    public GeneratePayrollHandler(IPayrollProcessingService payrollService)
    {
      _payrollService = payrollService;
    }

    public async Task<int> Handle(GeneratePayrollCommand request, CancellationToken cancellationToken)
    {
      // 1. Parse month format "MM-yyyy" (Already validated by FluentValidation)
      var parts = request.Month.Split('-');
      var monthStr = int.Parse(parts[0]).ToString("D2"); // Ensure 2 digits
      var yearStr = int.Parse(parts[1]).ToString();

      // 2. Delegate calculation to shared service (also used by PayrollBackgroundService)
      return await _payrollService.CalculatePayrollAsync(monthStr, yearStr);
    }
  }
}
