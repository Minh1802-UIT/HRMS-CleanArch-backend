using Employee.Application.Common.Exceptions;
using Employee.Domain.Common.Models;
using Employee.Application.Features.Payroll.Mappers;
using Employee.Application.Features.Payroll.Dtos;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;


namespace Employee.Application.Features.Payroll.Services
{
  public class PayrollService : IPayrollService
  {
    private readonly IPayrollRepository _payrollRepo;
    public PayrollService(IPayrollRepository payrollRepo)
    {
      _payrollRepo = payrollRepo;
    }

    public async Task<PayrollDto> CalculatePayrollAsync(string employeeId, string month)
    {
      var payroll = await _payrollRepo.GetByEmployeeAndMonthAsync(employeeId, month);
      if (payroll == null)
      {
        throw new NotFoundException("B?ng luong chua du?c tính cho nhân viên này. Vui lòng ch?y tính luong tru?c.");
      }

      return payroll.ToDto();
    }

    public async Task<PayrollDto?> GetMyPayrollAsync(string employeeId, string month)
    {
      var payroll = await _payrollRepo.GetByEmployeeAndMonthAsync(employeeId, month);
      if (payroll == null) return null;

      return payroll.ToDto();
    }

    public async Task<IEnumerable<PayrollDto>> GetMyHistoryAsync(string userId)
    {
      var list = await _payrollRepo.GetByEmployeeIdAsync(userId);
      return list.Select(p => p.ToDto()).ToList();
    }

    public async Task<PayrollDto> GetByIdAsync(string id)
    {
      var payroll = await _payrollRepo.GetByIdAsync(id);
      if (payroll == null) throw new NotFoundException($"Payroll with ID {id} not found.");

      return payroll.ToDto();
    }

    public async Task<IEnumerable<PayrollDto>> GetByEmployeeIdAsync(string userId)
    {
      var list = await _payrollRepo.GetByEmployeeIdAsync(userId);
      return list.Select(p => p.ToDto()).ToList();
    }

    public async Task<IEnumerable<PayrollDto>> GetByMonthAsync(string month)
    {
      var list = await _payrollRepo.GetByMonthAsync(month);

      return list.Select(p => p.ToDto()).ToList();
    }

    public async Task<PagedResult<PayrollListDto>> GetPagedListAsync(PaginationParams pagination)
    {
      var pagedPayrolls = await _payrollRepo.GetPagedAsync(pagination);

      var dtos = pagedPayrolls.Items.Select(p => new PayrollListDto
      {
        Id = p.Id,
        EmployeeCode = p.Snapshot.EmployeeCode,
        EmployeeName = p.Snapshot.EmployeeName,
        Month = p.Month,
        GrossIncome = p.GrossIncome,
        BaseSalary = p.BaseSalary,
        Allowances = p.Allowances,
        ActualWorkingDays = p.ActualWorkingDays,
        TotalDeductions = p.TotalDeductions,
        FinalNetSalary = p.FinalNetSalary,
        Status = p.Status.ToString(),
        PaidDate = p.PaidDate
      }).ToList();

      return new PagedResult<PayrollListDto>
      {
        Items = dtos,
        TotalCount = pagedPayrolls.TotalCount,
        PageNumber = pagedPayrolls.PageNumber,
        PageSize = pagedPayrolls.PageSize
      };
    }

    public async Task<PagedResult<PayrollListDto>> GetByMonthPagedAsync(string month, PaginationParams pagination)
    {
      var pagedPayrolls = await _payrollRepo.GetByMonthPagedAsync(month, pagination);

      var dtos = pagedPayrolls.Items.Select(p => new PayrollListDto
      {
        Id = p.Id,
        EmployeeCode = p.Snapshot.EmployeeCode,
        EmployeeName = p.Snapshot.EmployeeName,
        Month = p.Month,
        GrossIncome = p.GrossIncome,
        BaseSalary = p.BaseSalary,
        Allowances = p.Allowances,
        ActualWorkingDays = p.ActualWorkingDays,
        TotalDeductions = p.TotalDeductions,
        FinalNetSalary = p.FinalNetSalary,
        Status = p.Status.ToString(),
        PaidDate = p.PaidDate
      }).ToList();

      return new PagedResult<PayrollListDto>
      {
        Items = dtos,
        TotalCount = pagedPayrolls.TotalCount,
        PageNumber = pagedPayrolls.PageNumber,
        PageSize = pagedPayrolls.PageSize
      };
    }

    /// <summary>
    /// NEW-7: Aggregate all payroll records for a calendar year into an annual PIT report.
    /// Uses GetByMonthsAsync to fetch all 12 months in one batch.
    /// </summary>
    public async Task<AnnualTaxReportDto> GetAnnualTaxReportAsync(int year)
    {
      // Build month keys: ["01-2026", "02-2026", ..., "12-2026"]
      var months = Enumerable.Range(1, 12)
          .Select(m => $"{m:D2}-{year}")
          .ToList();

      var records = await _payrollRepo.GetByMonthsAsync(months);

      // Group by employee
      var grouped = records.GroupBy(r => r.EmployeeId);

      var employees = grouped.Select(g =>
      {
        var snapshot = g.First().Snapshot;
        var monthlySummaries = g.OrderBy(r => r.Month).Select(r => new MonthlyTaxSummaryDto
        {
          Month = r.Month,
          GrossIncome = r.GrossIncome,
          SocialInsurance = r.SocialInsurance,
          HealthInsurance = r.HealthInsurance,
          UnemploymentInsurance = r.UnemploymentInsurance,
          TotalInsurance = r.SocialInsurance + r.HealthInsurance + r.UnemploymentInsurance,
          TaxableIncome = r.GrossIncome - r.SocialInsurance - r.HealthInsurance - r.UnemploymentInsurance,
          PersonalIncomeTax = r.PersonalIncomeTax,
          TotalDeductions = r.TotalDeductions,
          FinalNetSalary = r.FinalNetSalary
        }).ToList();

        return new EmployeeTaxSummaryDto
        {
          EmployeeId = g.Key,
          EmployeeCode = snapshot.EmployeeCode,
          EmployeeName = snapshot.EmployeeName,
          Year = year,
          MonthlySummaries = monthlySummaries,
          TotalGrossIncome = monthlySummaries.Sum(m => m.GrossIncome),
          TotalSocialInsurance = monthlySummaries.Sum(m => m.SocialInsurance),
          TotalHealthInsurance = monthlySummaries.Sum(m => m.HealthInsurance),
          TotalUnemploymentInsurance = monthlySummaries.Sum(m => m.UnemploymentInsurance),
          TotalPersonalIncomeTax = monthlySummaries.Sum(m => m.PersonalIncomeTax),
          TotalNetSalary = monthlySummaries.Sum(m => m.FinalNetSalary)
        };
      }).OrderBy(e => e.EmployeeCode).ToList();

      return new AnnualTaxReportDto
      {
        Year = year,
        TotalEmployees = employees.Count,
        Employees = employees,
        CompanyTotalGross = employees.Sum(e => e.TotalGrossIncome),
        CompanyTotalPIT = employees.Sum(e => e.TotalPersonalIncomeTax),
        CompanyTotalNet = employees.Sum(e => e.TotalNetSalary),
        CompanyTotalInsurance = employees.Sum(e =>
            e.TotalSocialInsurance + e.TotalHealthInsurance + e.TotalUnemploymentInsurance)
      };
    }
  }
}
