using Employee.Domain.Entities.Common;
using System;

namespace Employee.Domain.Entities.Attendance
{
    public class Shift : BaseEntity
    {
    public string Name { get; private set; } = string.Empty; // Morning Shift, Afternoon Shift, Office Hours...
    public string Code { get; private set; } = string.Empty; // S01, S02...

    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }

    public TimeSpan BreakStartTime { get; private set; }
    public TimeSpan BreakEndTime { get; private set; }

    // Grace Period (minutes)
    public int GracePeriodMinutes { get; private set; } = 15;

    // Is this an overnight shift? (EndTime < StartTime)
    public bool IsOvernight { get; private set; } = false;

    // Total standard working hours for this shift (e.g., 8.0)
    public double StandardWorkingHours { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Private constructor for MongoDB
    private Shift() { }

    public Shift(string name, string code, TimeSpan start, TimeSpan end, TimeSpan breakStart, TimeSpan breakEnd, double standardHours, int gracePeriod = 15, bool isOvernight = false)
    {
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
      if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Code is required.");

      Name = name;
      Code = code;
      StartTime = start;
      EndTime = end;
      BreakStartTime = breakStart;
      BreakEndTime = breakEnd;
      StandardWorkingHours = standardHours;
      GracePeriodMinutes = gracePeriod;
      IsOvernight = isOvernight;
      IsActive = true;
    }

    public void UpdateDetails(string name, TimeSpan start, TimeSpan end, TimeSpan breakStart, TimeSpan breakEnd, double standardHours, int gracePeriod, bool isOvernight)
    {
      Name = name;
      StartTime = start;
      EndTime = end;
      BreakStartTime = breakStart;
      BreakEndTime = breakEnd;
      StandardWorkingHours = standardHours;
      GracePeriodMinutes = gracePeriod;
      IsOvernight = isOvernight;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    }
}
