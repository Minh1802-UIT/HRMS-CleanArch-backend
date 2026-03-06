using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Leave
{
  public class LeaveType : BaseEntity
  {
    public string Name { get; private set; } = string.Empty; // e.g., Annual Leave, Sick Leave
    public string Code { get; private set; } = string.Empty; // AL, SL
    public string Description { get; private set; } = string.Empty;

    // Default days allocated per year
    public int DefaultDaysPerYear { get; private set; } = 12;

    // Whether days are accrued monthly
    public bool IsAccrual { get; private set; } = true;

    // Accrual rate per month (e.g., 1.0 or 1.25)
    public double AccrualRatePerMonth { get; private set; } = 1.0;

    // Whether unused days can be carried forward to the next year
    public bool AllowCarryForward { get; private set; } = false;

    // Maximum days that can be carried forward
    public int MaxCarryForwardDays { get; private set; } = 0;

    // Whether the Sandwich Rule applies (e.g., Fri-Mon leave costs 4 days)
    public bool IsSandwichRuleApplied { get; private set; } = false;

    public bool IsActive { get; private set; } = true;

    private LeaveType() { }

    public LeaveType(string name, string code, int defaultDays)
    {
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
      if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.");

      Name = name;
      Code = code;
      DefaultDaysPerYear = defaultDays;
      CreatedAt = DateTime.UtcNow;
    }

    public void UpdateSettings(bool isAccrual, double rate, bool allowCarryForward, int maxCarry, bool isSandwichRuleApplied = false)
    {
      IsAccrual = isAccrual;
      AccrualRatePerMonth = rate;
      AllowCarryForward = allowCarryForward;
      MaxCarryForwardDays = maxCarry;
      IsSandwichRuleApplied = isSandwichRuleApplied;
    }

    public void SetActive(bool isActive)
    {
      IsActive = isActive;
    }
  }
}
