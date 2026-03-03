using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.ValueObjects
{
  public class DailyLog
  {
    public DateTime Date { get; internal set; } // Working date

    // Actual times from RawLog
    public DateTime? CheckIn { get; internal set; }
    public DateTime? CheckOut { get; internal set; }

    public string ShiftCode { get; internal set; } = string.Empty; // Shift assigned for the day

    // Calculated results
    public double WorkingHours { get; internal set; }
    public int LateMinutes { get; internal set; } // Minutes late
    public int EarlyLeaveMinutes { get; internal set; } // Minutes early
    public double OvertimeHours { get; internal set; } // Overtime hours

    // Status: Present, Absent, Late, EarlyLeave, Leave, Holiday
    public AttendanceStatus Status { get; internal set; } = AttendanceStatus.Absent;

    public string Note { get; internal set; } = string.Empty;

    public bool IsHoliday { get; internal set; }
    public bool IsWeekend { get; internal set; }

    // Parameterless constructor for MongoDB deserialization.
    // Initializes all fields so GetUninitializedObject() is never needed.
    private DailyLog()
    {
      ShiftCode = string.Empty;
      Note = string.Empty;
      Status = AttendanceStatus.Absent;
    }

    // Constructor for creation and MongoDB
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