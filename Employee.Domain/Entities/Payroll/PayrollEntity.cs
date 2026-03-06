using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.Payroll
{
  public class PayrollEntity : BaseEntity
  {
    public string EmployeeId { get; private set; } = string.Empty;
    public string Month { get; private set; } = string.Empty; // Format: 02-2026

    // ==========================================
    // 1. INCOME SNAPSHOT (Locked at calculation time)
    // ==========================================
    public decimal BaseSalary { get; private set; }
    public decimal Allowances { get; private set; }
    public decimal Bonus { get; private set; }
    public decimal OvertimePay { get; private set; }
    public double OvertimeHours { get; private set; }

    // ==========================================
    // 2. ATTENDANCE DATA
    // ==========================================
    public double TotalWorkingDays { get; private set; }  // Standard working days
    public double ActualWorkingDays { get; private set; } // Actual working days
    public double UnpaidLeaveDays { get; private set; }   // Unpaid leave
    public double PayableDays { get; private set; }       // Days used for salary calculation

    // ==========================================
    // 3. CALCULATION RESULTS
    // ==========================================
    public decimal GrossIncome { get; private set; } // Total income before tax
    public decimal SocialInsurance { get; private set; }       // SI (8%)
    public decimal HealthInsurance { get; private set; }       // HI (1.5%)
    public decimal UnemploymentInsurance { get; private set; } // UI (1%)
    public decimal PersonalIncomeTax { get; private set; }     // PIT
    public decimal TotalDeductions { get; private set; }
    public decimal DebtAmount { get; private set; } // Debt amount (if negative, carries to next month)
    public decimal DebtPaid { get; private set; }   // Debt amount paid in this period
    public decimal FinalNetSalary { get; private set; } // Final net salary

    // ==========================================
    // 4. STATUS
    // ==========================================
    public PayrollStatus Status { get; private set; } = PayrollStatus.Draft;
    public DateTime? PaidDate { get; private set; }

    // ==========================================
    // 5. SNAPSHOT - Employee Info at Payroll Time
    // ==========================================
    public EmployeeSnapshot Snapshot { get; private set; } = new();

    // Constructor for EF/Persistence
    private PayrollEntity() { }

    public PayrollEntity(string employeeId, string month)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(month)) throw new ArgumentException("Month is required.");

      EmployeeId = employeeId;
      Month = month;
      Status = PayrollStatus.Draft;
      CreatedAt = DateTime.UtcNow;
    }

    public void UpdateAttendance(double total, double actual, double unpaid, double payable)
    {
      TotalWorkingDays = total;
      ActualWorkingDays = actual;
      UnpaidLeaveDays = unpaid;
      PayableDays = payable;
    }

    public void UpdateIncome(decimal baseSalary, decimal allowances, decimal bonus, decimal otPay, double otHours, decimal grossIncome)
    {
      BaseSalary = baseSalary;
      Allowances = allowances;
      Bonus = bonus;
      OvertimePay = otPay;
      OvertimeHours = otHours;

      GrossIncome = grossIncome;
    }

    public void UpdateDeductions(decimal si, decimal hi, decimal ui, decimal pit, decimal debtPaid)
    {
      SocialInsurance = si;
      HealthInsurance = hi;
      UnemploymentInsurance = ui;
      PersonalIncomeTax = pit;
      DebtPaid = debtPaid;

      TotalDeductions = si + hi + ui + pit + debtPaid;
    }

    public void FinalizeCalculation(decimal net, decimal debtAmount)
    {
      FinalNetSalary = net;
      DebtAmount = debtAmount;
    }

    public void UpdateSnapshot(EmployeeSnapshot snapshot)
    {
      Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public void Approve()
    {
      if (Status != PayrollStatus.Draft) throw new InvalidOperationException("Only Draft payroll can be approved.");
      Status = PayrollStatus.Approved;
    }

    public void MarkAsPaid(DateTime date)
    {
      if (Status != PayrollStatus.Approved) throw new InvalidOperationException("Only Approved payroll can be marked as paid.");
      Status = PayrollStatus.Paid;
      PaidDate = date;
    }

    public void RevertToApproved()
    {
      if (Status != PayrollStatus.Paid) throw new InvalidOperationException("Only Paid payroll can be reverted to Approved.");
      Status = PayrollStatus.Approved;
      PaidDate = null;
    }

    public void Reject()
    {
      if (Status == PayrollStatus.Paid)
        throw new InvalidOperationException("Cannot reject a payroll that has already been paid.");
      if (Status == PayrollStatus.Rejected)
        throw new InvalidOperationException("Payroll is already rejected.");
      Status = PayrollStatus.Rejected;
    }
  }

  public record EmployeeSnapshot
  {
    public string EmployeeName { get; init; } = string.Empty;
    public string EmployeeCode { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public string PositionTitle { get; init; } = string.Empty;
  }
}