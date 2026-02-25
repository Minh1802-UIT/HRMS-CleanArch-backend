using Employee.Domain.Entities.Common;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Domain.Entities.Attendance
{
  public class AttendanceBucket : BaseEntity
  {
    public string EmployeeId { get; private set; } = string.Empty;

    // Month identifier: "01-2026", "02-2026"...
    public string Month { get; private set; } = string.Empty;

    // List of daily logs for the month
    private readonly List<DailyLog> _dailyLogs = new();
    public IReadOnlyCollection<DailyLog> DailyLogs => _dailyLogs.AsReadOnly();

    // Summary totals
    public int TotalPresent { get; private set; }
    public int TotalLate { get; private set; }
    public double TotalOvertime { get; private set; }

    // Private constructor for MongoDB
    private AttendanceBucket() { }

    public AttendanceBucket(string employeeId, string month)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(month)) throw new ArgumentException("Month is required.");

      EmployeeId = employeeId;
      Month = month;
    }

    public void AddOrUpdateDailyLog(DailyLog log)
    {
      var existing = _dailyLogs.FirstOrDefault(x => x.Date.Date == log.Date.Date);
      if (existing != null)
      {
        _dailyLogs.Remove(existing);
      }
      _dailyLogs.Add(log);
      RecalculateTotals();
    }

    public void RecalculateTotals()
    {
      TotalPresent = _dailyLogs.Count(x =>
        x.Status == AttendanceStatus.Present ||
        x.Status == AttendanceStatus.Late ||
        x.Status == AttendanceStatus.EarlyLeave);

      TotalLate = _dailyLogs.Count(x => x.Status == AttendanceStatus.Late);
      TotalOvertime = _dailyLogs.Sum(x => x.OvertimeHours);
    }
  }
}