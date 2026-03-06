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

    // Base status: Present | Absent | Leave | Holiday
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Absent;

    // Violation flags — additive, independent of each other.
    // Replaces using AttendanceStatus.Late / AttendanceStatus.EarlyLeave so that
    // combined violations (e.g. late AND early-leave the same day) are representable.
    public bool IsLate { get; set; }
    public bool IsEarlyLeave { get; set; }

    // Set by Ghost-Log auto-close: employee checked in but never checked out.
    public bool IsMissingPunch { get; set; }

    // Set when employee has a check-out but no check-in on that day after all recovery attempts.
    public bool IsMissingCheckIn { get; set; }

    // Computed convenience — true when the employee was physically present.
    // Also matches legacy Late/EarlyLeave status values stored in old MongoDB documents
    // before the boolean-flag refactor (those records count as present too).
#pragma warning disable CS0618 // legacy enum values intentionally supported here
    public bool IsPresent => Status == AttendanceStatus.Present
                          || Status == AttendanceStatus.Late
                          || Status == AttendanceStatus.EarlyLeave;
#pragma warning restore CS0618

    public string Note { get; set; } = string.Empty;

    public bool IsHoliday { get; set; }
    public bool IsWeekend { get; set; }

    // Only ONE public constructor: parameterless.
    // MongoDB Driver 3.x selects the constructor with the MOST parameters when
    // multiple public ctors exist. With only ONE ctor, there is no ambiguity —
    // the driver MUST use this and then populate all properties via public setters.
    public DailyLog()
    {
      ShiftCode = string.Empty;
      Note = string.Empty;
      Status = AttendanceStatus.Absent;
    }

    // Static factory — replaces the removed 2-param public constructor so that
    // application code remains readable while MongoDB deserialization is unambiguous.
    public static DailyLog Create(DateTime date, AttendanceStatus status = AttendanceStatus.Absent)
    {
      return new DailyLog
      {
        Date = date,
        // Only base statuses accepted here; violation flags are set via UpdateCalculationResults.
        Status = status,
        IsWeekend = (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
      };
    }

    public void UpdateCheckTimes(DateTime? checkIn, DateTime? checkOut, string shiftCode)
    {
      CheckIn = checkIn;
      CheckOut = checkOut;
      ShiftCode = shiftCode;
    }

    /// <summary>
    /// Updates all calculated fields on this log.
    /// Pass <paramref name="status"/> as the BASE status only (Present / Absent / Leave / Holiday).
    /// Use the boolean flags for combined violation tracking.
    /// </summary>
    public void UpdateCalculationResults(
      double workingHours,
      int lateMinutes,
      int earlyLeaveMinutes,
      double overtimeHours,
      AttendanceStatus status,
      string note = "",
      bool isLate = false,
      bool isEarlyLeave = false,
      bool isMissingPunch = false,
      bool isMissingCheckIn = false)
    {
      WorkingHours = workingHours;
      LateMinutes = lateMinutes;
      EarlyLeaveMinutes = earlyLeaveMinutes;
      OvertimeHours = overtimeHours;
      Status = status;
      Note = note;
      IsLate = isLate;
      IsEarlyLeave = isEarlyLeave;
      IsMissingPunch = isMissingPunch;
      IsMissingCheckIn = isMissingCheckIn;
    }

    /// <summary>
    /// Marks this day as a public holiday.
    /// - If the employee has NO check-in (absent): sets Status=Holiday so it counts as paid holiday.
    /// - If the employee IS present (checked in): only sets the IsHoliday flag; Status stays Present
    ///   so working-hours / OT / present-count remain intact (holiday OT rate applied separately).
    /// </summary>
    public void SetHoliday(bool isHoliday, string note = "Holiday")
    {
      IsHoliday = isHoliday;
      if (isHoliday)
      {
        Note = string.IsNullOrEmpty(Note) ? note : $"{Note} · {note}";
        // Only override status when the day is a true absence (no punch).
        // Present employees keep their calculated status so TotalPresent stays correct.
        if (!CheckIn.HasValue)
        {
          Status = AttendanceStatus.Holiday;
          IsLate = false;
          IsEarlyLeave = false;
          IsMissingPunch = false;
        }
      }
    }

    public void SetLeave(AttendanceStatus leaveStatus, string note)
    {
      Status = leaveStatus;
      Note = note;
      IsLate = false;
      IsEarlyLeave = false;
      IsMissingPunch = false;
    }
  }
}