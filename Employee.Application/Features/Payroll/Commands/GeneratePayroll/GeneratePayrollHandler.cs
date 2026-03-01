using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService; // Fix Namespace
using MediatR;
using Employee.Domain.Common.Models;

namespace Employee.Application.Features.Payroll.Commands.GeneratePayroll
{
  public class GeneratePayrollHandler : IRequestHandler<GeneratePayrollCommand, int>
  {
    private readonly IPayrollProcessingService _payrollService;

    public GeneratePayrollHandler(IPayrollProcessingService payrollService)
    {
      _payrollService = payrollService;
    }

    public async Task<int> Handle(GeneratePayrollCommand request, CancellationToken cancellationToken)
    {
      // request.Month format: "MM-yyyy"
      var parts = request.Month.Split('-');
      if (parts.Length != 2) return 0;

      string month = parts[0];
      string year = parts[1];

      return await _payrollService.CalculatePayrollAsync(month, year);
    }
  }
}
