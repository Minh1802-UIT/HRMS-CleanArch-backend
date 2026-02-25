namespace Employee.Application.Features.Payroll.Dtos;

public class PayrollListDto
{
  public string Id { get; set; } = string.Empty;
  public string EmployeeCode { get; set; } = string.Empty;
  public string EmployeeName { get; set; } = string.Empty;
  public string Month { get; set; } = string.Empty;
  public decimal GrossIncome { get; set; }
  public decimal BaseSalary { get; set; }
  public decimal Allowances { get; set; }
  public double ActualWorkingDays { get; set; }
  public decimal TotalDeductions { get; set; }
  public decimal FinalNetSalary { get; set; }
  public string Status { get; set; } = string.Empty;
  public DateTime? PaidDate { get; set; }
}
