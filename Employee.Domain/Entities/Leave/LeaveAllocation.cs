using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Leave
{
  public class LeaveAllocation : BaseEntity
  {
    public string EmployeeId { get; private set; } = string.Empty;
    public string LeaveTypeId { get; private set; } = string.Empty; // FK to LeaveType
    public string Year { get; private set; } = DateTime.UtcNow.Year.ToString(); // Managed by year: "2026"

    // Days allocated at start of period (or carried from last year)
    public double NumberOfDays { get; private set; }

    // Additional days accrued during the year (for Accrual types)
    public double AccruedDays { get; private set; }

    // Days already used
    public double UsedDays { get; private set; }

    // Last month that was automatically accrued (e.g., "2026-02")
    // Used to prevent double accrual in the same month.
    public string? LastAccrualMonth { get; private set; }

    // Current balance = (NumberOfDays + AccruedDays) - UsedDays
    public double CurrentBalance => NumberOfDays + AccruedDays - UsedDays;

    private LeaveAllocation() { }

    public LeaveAllocation(string employeeId, string leaveTypeId, string year, double initialDays)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(leaveTypeId)) throw new ArgumentException("LeaveTypeId is required.");

      EmployeeId = employeeId;
      LeaveTypeId = leaveTypeId;
      Year = year;
      NumberOfDays = initialDays;
    }

    public void AddAccrual(double days, string month)
    {
      if (LastAccrualMonth == month) return;
      AccruedDays += days;
      LastAccrualMonth = month;
    }

    public void RecordUsage(double days)
    {
      if (days > CurrentBalance) throw new InvalidOperationException("Insufficient leave balance.");
      UsedDays += days;
    }

    public void UpdateUsedDays(double days)
    {
      if (days > 0 && days > CurrentBalance)
        throw new InvalidOperationException("Insufficient leave balance.");
      UsedDays += days;
      if (UsedDays < 0) UsedDays = 0; // Safety guard for refund edge cases
    }

    public void UpdateAllocation(double numberOfDays)
    {
      NumberOfDays = numberOfDays;
    }

    public void UpdateAccrual(double accruedDays, string lastAccrualMonth)
    {
      AccruedDays = accruedDays;
      LastAccrualMonth = lastAccrualMonth;
    }

    public void RefundUsage(double days)
    {
      if (days <= 0) throw new ArgumentException("Refund days must be positive.");
      UsedDays = Math.Max(0, UsedDays - days);
    }
  }
}
