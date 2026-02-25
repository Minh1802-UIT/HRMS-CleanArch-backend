namespace Employee.Domain.Services.Payroll
{
  public class VietnameseTaxCalculator : ITaxCalculator
  {
    public decimal CalculatePersonalIncomeTax(decimal taxableIncome)
    {
      if (taxableIncome <= 0) return 0;

      // Vietnamese PIT Progressive Tax Rates (7 Steps)
      // 1. Up to 5M: 5%
      if (taxableIncome <= 5000000)
        return taxableIncome * 0.05m;

      // 2. 5M to 10M: 10% minus 0.25M
      if (taxableIncome <= 10000000)
        return taxableIncome * 0.10m - 250000;

      // 3. 10M to 18M: 15% minus 0.75M
      if (taxableIncome <= 18000000)
        return taxableIncome * 0.15m - 750000;

      // 4. 18M to 32M: 20% minus 1.65M
      if (taxableIncome <= 32000000)
        return taxableIncome * 0.20m - 1650000;

      // 5. 32M to 52M: 25% minus 3.25M
      if (taxableIncome <= 52000000)
        return taxableIncome * 0.25m - 3250000;

      // 6. 52M to 80M: 30% minus 5.85M
      if (taxableIncome <= 80000000)
        return taxableIncome * 0.30m - 5850000;

      // 7. Above 80M: 35% minus 9.85M
      return taxableIncome * 0.35m - 9850000;
    }
  }
}
