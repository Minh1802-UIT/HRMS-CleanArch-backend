using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Enums;

using Employee.Application.Common.Models;

using Employee.Domain.Services.Payroll;
using Microsoft.Extensions.Logging;

namespace Employee.Application.Features.Payroll.Services
{
  public class PayrollProcessingService : IPayrollProcessingService
  {
    private readonly IPayrollRepository _payrollRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPayrollDataProvider _dataProvider;
    private readonly ITaxCalculator _taxCalculator;
    private readonly ILogger<PayrollProcessingService> _logger;

    public PayrollProcessingService(
        IPayrollRepository payrollRepo,
        IUnitOfWork unitOfWork,
        IPayrollDataProvider dataProvider,
        ITaxCalculator taxCalculator,
        ILogger<PayrollProcessingService> logger)
    {
      _payrollRepo = payrollRepo;
      _unitOfWork = unitOfWork;
      _dataProvider = dataProvider;
      _taxCalculator = taxCalculator;
      _logger = logger;
    }

    public async Task<int> CalculatePayrollAsync(string month, string year)
    {
      var data = await _dataProvider.FetchCalculationDataAsync(month, year);
      int count = 0;

      await _unitOfWork.BeginTransactionAsync();
      try
      {
        foreach (var emp in data.Employees)
        {
          if (!data.SalaryMap.TryGetValue(emp.Id, out var salaryInfo)) continue;

          var bucket = data.AttendanceMap.GetValueOrDefault(emp.Id);
          var prevPayroll = data.PrevPayrollMap.GetValueOrDefault(emp.Id);

          // 1. Calculate Income
          decimal baseSalary = salaryInfo.BasicSalary;
          decimal allowances = salaryInfo.TransportAllowance + salaryInfo.LunchAllowance + salaryInfo.OtherAllowance;
          decimal overtimeHours = (decimal)(bucket?.TotalOvertime ?? 0);
          decimal standardWorkingDays = (decimal)data.Settings.StandardWorkingDays;
          if (standardWorkingDays <= 0) standardWorkingDays = 22; // Safe fallback
          decimal hourlyRate = baseSalary / standardWorkingDays / 8;
          decimal overtimePay = overtimeHours * hourlyRate * data.Settings.OvertimeRateNormal;

          double actualPayableDays = bucket?.TotalPresent ?? 0;
          decimal grossIncome = ((baseSalary + allowances) / (decimal)data.Settings.StandardWorkingDays * (decimal)actualPayableDays) + overtimePay;

          // 2. Insurance & Tax
          decimal insuranceSalary = Math.Min(baseSalary, data.Settings.InsuranceSalaryCap);
          decimal bhxh = insuranceSalary * data.Settings.SocialInsuranceRate;
          decimal bhyt = insuranceSalary * data.Settings.HealthInsuranceRate;
          decimal bhtn = insuranceSalary * data.Settings.UnemploymentInsuranceRate;

          decimal incomeBeforeTax = grossIncome - (bhxh + bhyt + bhtn);
          decimal personalDeductionTotal = data.Settings.PersonalDeduction + (emp.PersonalInfo.DependentCount * data.Settings.DependentDeduction);
          decimal taxableIncome = Math.Max(0, incomeBeforeTax - personalDeductionTotal);
          decimal tax = _taxCalculator.CalculatePersonalIncomeTax(taxableIncome);

          // 3. Debt & Net
          decimal debtPaid = prevPayroll?.DebtAmount ?? 0;
          decimal netSalary = grossIncome - (bhxh + bhyt + bhtn + tax) - debtPaid;
          decimal newDebt = netSalary < 0 ? Math.Abs(netSalary) : 0;
          if (netSalary < 0) netSalary = 0;

          // 4. Persistence
          var payroll = data.CurrentPayrollMap.TryGetValue(emp.Id, out var existing)
              ? existing
              : new PayrollEntity(emp.Id, data.MonthKey);

          payroll.UpdateAttendance(data.Settings.StandardWorkingDays, actualPayableDays, 0, actualPayableDays);
          payroll.UpdateIncome(baseSalary, allowances, 0, overtimePay, (double)overtimeHours);
          payroll.UpdateDeductions(bhxh, bhyt, bhtn, tax, debtPaid);
          payroll.FinalizeCalculation(netSalary, newDebt);

          var snapshot = new EmployeeSnapshot
          {
            EmployeeName = emp.FullName,
            EmployeeCode = emp.EmployeeCode,
            DepartmentName = emp.JobDetails?.DepartmentId != null && data.DeptNames.TryGetValue(emp.JobDetails.DepartmentId, out var dName) ? dName : "Unknown",
            PositionTitle = emp.JobDetails?.PositionId != null && data.PositionNames.TryGetValue(emp.JobDetails.PositionId, out var pTitle) ? pTitle : "Unknown"
          };
          payroll.UpdateSnapshot(snapshot);

          if (payroll.Status == PayrollStatus.Draft)
          {
            if (string.IsNullOrEmpty(payroll.Id))
              await _payrollRepo.CreateAsync(payroll);
            else
              await _payrollRepo.UpdateAsync(payroll.Id, payroll);
          }
          count++;
        }

        await _unitOfWork.CommitTransactionAsync();
        return count;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Payroll calculation failed for {Month}-{Year}. Rolling back transaction.", month, year);
        await _unitOfWork.RollbackTransactionAsync();
        throw;
      }
    }

    public async Task FinalizePayrollAsync(string month, string year)
    {
      var monthKey = $"{month}-{year}";
      var approvedCount = await _payrollRepo.ApproveDraftsByMonthAsync(monthKey);
      _logger.LogInformation("Finalized {Count} payrolls for {MonthKey}.", approvedCount, monthKey);
    }

  }
}
