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

    // Parameterless constructor for MongoDB deserialization
    public AttendanceBucket() { DailyLogs = new List<DailyLog>(); }

    public AttendanceBucket(string employeeId, string month)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(month)) throw new ArgumentException("Month is required.");

      EmployeeId = employeeId;
      Month = month;
      DailyLogs = new List<DailyLog>();
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
      TotalPresent = logs.Count(x =>
        x.Status == AttendanceStatus.Present ||
        x.Status == AttendanceStatus.Late ||
        x.Status == AttendanceStatus.EarlyLeave);

      TotalLate = logs.Count(x => x.Status == AttendanceStatus.Late);
      TotalOvertime = logs.Sum(x => x.OvertimeHours);
    }
  }
}