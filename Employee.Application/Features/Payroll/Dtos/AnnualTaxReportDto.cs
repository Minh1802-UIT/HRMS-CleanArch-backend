using System.Collections.Generic;

namespace Employee.Application.Features.Payroll.Dtos
{
  /// <summary>Monthly PIT breakdown for one employee.</summary>
  public class MonthlyTaxSummaryDto
  {
    public string Month { get; set; } = string.Empty;     // "01-2026"
    public decimal GrossIncome { get; set; }
    public decimal SocialInsurance { get; set; }
    public decimal HealthInsurance { get; set; }
    public decimal UnemploymentInsurance { get; set; }
    public decimal TotalInsurance { get; set; }
    public decimal TaxableIncome { get; set; }            // GrossIncome - TotalInsurance - PersonalDeduction
    public decimal PersonalIncomeTax { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal FinalNetSalary { get; set; }
  }

  /// <summary>Annual personal-income-tax summary for one employee.</summary>
  public class EmployeeTaxSummaryDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Year { get; set; }

    public List<MonthlyTaxSummaryDto> MonthlySummaries { get; set; } = new();

    // Totals across all months
    public decimal TotalGrossIncome { get; set; }
    public decimal TotalSocialInsurance { get; set; }
    public decimal TotalHealthInsurance { get; set; }
    public decimal TotalUnemploymentInsurance { get; set; }
    public decimal TotalPersonalIncomeTax { get; set; }
    public decimal TotalNetSalary { get; set; }
  }

  /// <summary>Full annual PIT report for all employees in a given year.</summary>
  public class AnnualTaxReportDto
  {
    public int Year { get; set; }
    public int TotalEmployees { get; set; }
    public List<EmployeeTaxSummaryDto> Employees { get; set; } = new();

    // Company-wide totals
    public decimal CompanyTotalGross { get; set; }
    public decimal CompanyTotalPIT { get; set; }
    public decimal CompanyTotalNet { get; set; }
    public decimal CompanyTotalInsurance { get; set; }
  }
}
