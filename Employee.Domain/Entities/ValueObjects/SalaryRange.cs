namespace Employee.Domain.Entities.ValueObjects
{
  public record SalaryRange
    {
    public decimal Min { get; init; }
    public decimal Max { get; init; }
    public string Currency { get; init; } = "VND";
    }
}