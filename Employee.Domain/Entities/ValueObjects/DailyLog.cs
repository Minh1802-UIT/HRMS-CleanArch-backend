using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.ValueObjects
{
  public class DailyLog
  {
    // Properties use public set so MongoDB C# driver can deserialize them
    // across assembly boundaries (internal set breaks Expression.Compile).
    // Domain mutation is still controlled through the Update* methods below.
    public DateTime Date { get; set; }

    // Actual times from RawLog
    public DateTime? CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }

    public string ShiftCode { get; set; } = string.Empty;

    // Calculated results
    public double WorkingHours { get; set; }
    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }
    public double OvertimeHours { get; set; }

    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

    public string Note { get; set; } = string.Empty;

    public bool IsHoliday { get; set; }
    public bool IsWeekend { get; set; }

    // Parameterless constructor for MongoDB deserialization.
    // PUBLIC so MongoDB.Bson (different assembly) can call it directly,
    // then sets each property via the public setters — no cross-assembly issues.
    public DailyLog()
    {
      ShiftCode = string.Empty;
      Note = string.Empty;
      Status = AttendanceStatus.Absent;
    }

    // Constructor for application use
    public DailyLog(DateTime date, AttendanceStatus status = AttendanceStatus.Absent)
    {
      Date = date;
      Status = status;
      IsWeekend = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
    }

    public void UpdateCheckTimes(DateTime? checkIn, DateTime? checkOut, string shiftCode)
    {
      CheckIn = checkIn;
      CheckOut = checkOut;
      ShiftCode = shiftCode;
    }

    public void UpdateCalculationResults(double workingHours, int lateMinutes, int earlyLeaveMinutes, double overtimeHours, AttendanceStatus status, string note = "")
    {
      WorkingHours = workingHours;
      LateMinutes = lateMinutes;
      EarlyLeaveMinutes = earlyLeaveMinutes;
      OvertimeHours = overtimeHours;
      Status = status;
      Note = note;
    }

    public void SetHoliday(bool isHoliday, string note = "Holiday")
    {
      IsHoliday = isHoliday;
      if (isHoliday)
      {
        Status = AttendanceStatus.Holiday;
        Note = note;
      }
    }

    public void SetLeave(AttendanceStatus leaveStatus, string note)
    {
      Status = leaveStatus;
      Note = note;
    }
  }
}