namespace Employee.Application.Common.Models;

/// <summary>
/// Lightweight projection for contract salary information used in payroll processing
/// </summary>
public class ContractSalaryProjection
{
  public string EmployeeId { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public decimal BasicSalary { get; set; }
  public decimal TransportAllowance { get; set; }
  public decimal LunchAllowance { get; set; }
}
