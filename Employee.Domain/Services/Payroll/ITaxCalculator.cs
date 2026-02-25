namespace Employee.Domain.Services.Payroll
{
  public interface ITaxCalculator
  {
    decimal CalculatePersonalIncomeTax(decimal taxableIncome);
  }
}
