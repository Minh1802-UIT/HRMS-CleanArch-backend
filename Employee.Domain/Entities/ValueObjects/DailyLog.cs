using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.ValueObjects
{
  public class DailyLog
  {
    public DateTime Date { get; private set; } // Working date

    // Actual times from RawLog
    public DateTime? CheckIn { get; private set; }
    public DateTime? CheckOut { get; private set; }

    public string ShiftCode { get; private set; } = string.Empty; // Shift assigned for the day

    // Calculated results
    public double WorkingHours { get; private set; }
    public int LateMinutes { get; private set; } // Minutes late
    public int EarlyLeaveMinutes { get; private set; } // Minutes early
    public double OvertimeHours { get; private set; } // Overtime hours

    // Status: Present, Absent, Late, EarlyLeave, Leave, Holiday
    public AttendanceStatus Status { get; private set; } = AttendanceStatus.Absent;

    public string Note { get; private set; } = string.Empty;

    public bool IsHoliday { get; private set; }
    public bool IsWeekend { get; private set; }

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