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
      // 1. Validate & parse month format "MM-yyyy"
      var parts = request.Month.Split('-');
      if (parts.Length != 2
          || !int.TryParse(parts[0], out var month)
          || !int.TryParse(parts[1], out var year)
          || month < 1 || month > 12
          || year < 2000 || year > 2100)
      {
        throw new ValidationException("Invalid month format. Expected MM-yyyy (e.g., 01-2026).",
            new List<string> { $"Month: '{request.Month}' is not valid." });
      }

      // 2. Delegate calculation to shared service (also used by PayrollBackgroundService)
      return await _payrollService.CalculatePayrollAsync(
          month.ToString("D2"), year.ToString());
    }
  }
}
