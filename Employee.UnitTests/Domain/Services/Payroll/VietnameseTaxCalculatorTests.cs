using Employee.Domain.Services.Payroll;
using Xunit;

namespace Employee.UnitTests.Domain.Services.Payroll
{
    public class VietnameseTaxCalculatorTests
    {
        private readonly VietnameseTaxCalculator _calculator;

        public VietnameseTaxCalculatorTests()
        {
            _calculator = new VietnameseTaxCalculator();
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(-1000, 0)]
        [InlineData(5000000, 250000)] // Step 1: 5% of 5M
        [InlineData(8000000, 550000)] // Step 2: 8M * 10% - 250k = 800k - 250k = 550k
        [InlineData(15000000, 1500000)] // Step 3: 15M * 15% - 750k = 2.25M - 750k = 1.5M
        [InlineData(25000000, 3350000)] // Step 4: 25M * 20% - 1.65M = 5M - 1.65M = 3.35M
        [InlineData(40000000, 6750000)] // Step 5: 40M * 25% - 3.25M = 10M - 3.25M = 6.75M
        [InlineData(60000000, 12150000)] // Step 6: 60M * 30% - 5.85M = 18M - 5.85M = 12.15M
        [InlineData(100000000, 25150000)] // Step 7: 100M * 35% - 9.85M = 35M - 9.85M = 25.15M
        public void CalculatePersonalIncomeTax_ShouldReturnCorrectAmount(decimal income, decimal expectedTax)
        {
            // Act
            var result = _calculator.CalculatePersonalIncomeTax(income);

            // Assert
            Assert.Equal(expectedTax, result);
        }
    }
}
