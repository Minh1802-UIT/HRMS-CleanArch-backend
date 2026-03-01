namespace Employee.Application.Common.Dtos;

/// <summary>
/// Lightweight projection for contract salary information used in payroll processing.
/// Moved from Employee.Domain.Common.Models — projections belong in Application layer.
/// </summary>
public class ContractSalaryProjection
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal BasicSalary { get; set; }
    public decimal TransportAllowance { get; set; }
    public decimal LunchAllowance { get; set; }
    public decimal OtherAllowance { get; set; }
}
