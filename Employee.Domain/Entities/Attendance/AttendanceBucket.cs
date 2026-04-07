using Employee.Domain.Entities.Common;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Domain.Entities.Attendance
{
  public class AttendanceBucket : BaseEntity
  {
    // public set on all properties so MongoDB AutoMap can deserialize across assemblies
    public string EmployeeId { get; set; } = string.Empty;

    // Month identifier: "01-2026", "02-2026"...
    public string Month { get; set; } = string.Empty;

    // Public List with setter so MongoDB AutoMap deserializes the array directly.
    // Previously private _dailyLogs + IReadOnlyCollection — the private-field mapping
    // via MapField() is unreliable on Linux (Render) because reflection-based field
    // access behaves differently from Windows in some .NET versions.
    public List<DailyLog> DailyLogs { get; set; } = new();

    // Summary totals
    public int TotalPresent { get; set; }
    public int TotalLate { get; set; }
    public double TotalOvertime { get; set; }

    // Compensatory Time Balance tracking (Scoped to this month only)
    public double UsedCompensatoryHours { get; set; }
    public double PendingCompensatoryHours { get; set; }
    public double AvailableCompensatoryHours => TotalOvertime > 0 ? (TotalOvertime - UsedCompensatoryHours - PendingCompensatoryHours) : 0;

    // Parameterless constructor for MongoDB deserialization
    public AttendanceBucket() { DailyLogs = new List<DailyLog>(); }

    public AttendanceBucket(string employeeId, string month)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(month)) throw new ArgumentException("Month is required.");

      EmployeeId = employeeId;
      Month = month;
      DailyLogs = new List<DailyLog>();
      CreatedAt = DateTime.UtcNow;
    }

    public void AddOrUpdateDailyLog(DailyLog log)
    {
      DailyLogs ??= new List<DailyLog>();
      var existing = DailyLogs.FirstOrDefault(x => x.Date.Date == log.Date.Date);
      if (existing != null)
      {
        DailyLogs.Remove(existing);
      }
      DailyLogs.Add(log);
      RecalculateTotals();
    }

    public void RecalculateTotals()
    {
      var logs = DailyLogs ?? Enumerable.Empty<DailyLog>();
      // IsPresent is a computed property: Status == Present
      TotalPresent = logs.Count(x => x.IsPresent);
      // TotalLate uses the new boolean flag (independent of base status)
      TotalLate = logs.Count(x => x.IsLate);
      TotalOvertime = logs.Sum(x => x.OvertimeHours);
    }

    public void ReserveCompensatoryHours(double hours)
    {
        if (hours <= 0) throw new ArgumentException("Hours must be greater than 0");
        if (AvailableCompensatoryHours < hours) 
            throw new InvalidOperationException($"Not enough available compensatory hours. Available: {AvailableCompensatoryHours}, Requested: {hours}");
        
        PendingCompensatoryHours += hours;
    }

    public void ConfirmCompensatoryHours(double hours)
    {
        if (hours <= 0) throw new ArgumentException("Hours must be greater than 0");
        
        PendingCompensatoryHours -= hours;
        if (PendingCompensatoryHours < 0) PendingCompensatoryHours = 0; // Safeguard

        UsedCompensatoryHours += hours;
    }

    public void CancelCompensatoryHours(double hours)
    {
        if (hours <= 0) throw new ArgumentException("Hours must be greater than 0");
        
        PendingCompensatoryHours -= hours;
        if (PendingCompensatoryHours < 0) PendingCompensatoryHours = 0; // Safeguard
    }
  }
}