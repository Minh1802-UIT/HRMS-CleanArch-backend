namespace Employee.Domain.Entities.ValueObjects
{
  public record SalaryComponents
    {
    public decimal BasicSalary { get; init; } // Basic salary (required)
    public decimal TransportAllowance { get; init; } // Transport allowance
    public decimal LunchAllowance { get; init; }     // Lunch allowance
    public decimal OtherAllowance { get; init; }     // Other allowances
  }
}