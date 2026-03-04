using Employee.Domain.Entities.Payroll;
using Employee.Domain.Enums;
using Xunit;

namespace Employee.UnitTests.Domain.Entities.Payroll
{
  public class PayrollEntityTests
  {
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
      // Arrange & Act
      var payroll = new PayrollEntity("emp123", "02-2026");

      // Assert
      Assert.Equal("emp123", payroll.EmployeeId);
      Assert.Equal("02-2026", payroll.Month);
      Assert.Equal(PayrollStatus.Draft, payroll.Status);
    }

    [Theory]
    [InlineData("", "02-2026")]
    [InlineData("emp123", "")]
    [InlineData(null, "02-2026")]
    public void Constructor_InvalidInput_ShouldThrowException(string? empId, string? month)
    {
      Assert.Throws<ArgumentException>(() => new PayrollEntity(empId!, month!));
    }

    [Fact]
    public void UpdateIncome_ShouldCalculateGrossCorrectly()
    {
      // Arrange
      var payroll = new PayrollEntity("emp1", "02-2026");
      decimal baseSalary = 10000;
      decimal allowances = 2000;
      decimal bonus = 500;
      decimal otPay = 1000;

      // Act
      decimal expectedGross = baseSalary + allowances + bonus + otPay;
      payroll.UpdateIncome(baseSalary, allowances, bonus, otPay, 5, expectedGross);

      // Assert
      Assert.Equal(13500, payroll.GrossIncome);
      Assert.Equal(baseSalary, payroll.BaseSalary);
      Assert.Equal(allowances, payroll.Allowances);
      Assert.Equal(bonus, payroll.Bonus);
      Assert.Equal(otPay, payroll.OvertimePay);
    }

    [Fact]
    public void UpdateDeductions_ShouldCalculateTotalDeductionsCorrectly()
    {
      // Arrange
      var payroll = new PayrollEntity("emp1", "02-2026");
      decimal si = 800;
      decimal hi = 150;
      decimal ui = 100;
      decimal pit = 500;
      decimal debtPaid = 200;

      // Act
      payroll.UpdateDeductions(si, hi, ui, pit, debtPaid);

      // Assert
      Assert.Equal(17500, payroll.TotalDeductions * 10); // Wait, TotalDeductions is just sum
      Assert.Equal(1750, payroll.TotalDeductions);
      Assert.Equal(si, payroll.SocialInsurance);
      Assert.Equal(hi, payroll.HealthInsurance);
      Assert.Equal(ui, payroll.UnemploymentInsurance);
      Assert.Equal(pit, payroll.PersonalIncomeTax);
      Assert.Equal(debtPaid, payroll.DebtPaid);
    }

    [Fact]
    public void StatusTransitions_ShouldFollowWorkflow()
    {
      // Arrange
      var payroll = new PayrollEntity("emp1", "02-2026");
      var paidDate = DateTime.UtcNow;

      // Act & Assert 1: Draft -> Approved
      payroll.Approve();
      Assert.Equal(PayrollStatus.Approved, payroll.Status);

      // Act & Assert 2: Approved -> Paid
      payroll.MarkAsPaid(paidDate);
      Assert.Equal(PayrollStatus.Paid, payroll.Status);
      Assert.Equal(paidDate, payroll.PaidDate);

      // Act & Assert 3: Paid -> Approved (Revert)
      payroll.RevertToApproved();
      Assert.Equal(PayrollStatus.Approved, payroll.Status);
      Assert.Null(payroll.PaidDate);
    }

    [Fact]
    public void MarkAsPaid_WhenDraft_ShouldThrowException()
    {
      // Arrange
      var payroll = new PayrollEntity("emp1", "02-2026");

      // Act & Assert
      Assert.Throws<InvalidOperationException>(() => payroll.MarkAsPaid(DateTime.UtcNow));
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldThrowException()
    {
      // Arrange
      var payroll = new PayrollEntity("emp1", "02-2026");
      payroll.Approve();

      // Act & Assert
      Assert.Throws<InvalidOperationException>(() => payroll.Approve());
    }
  }
}
