using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Attendance.Logic
{
  /// <summary>
  /// Pure calculation function: given a DailyLog with raw check-times and a Shift,
  /// computes WorkingHours, LateMinutes, EarlyLeaveMinutes, OvertimeHours and all
  /// violation flags. No side-effects; safe to call multiple times (idempotent).
  /// </summary>
  public class AttendanceCalculator
  {
    private readonly TimeZoneInfo _timeZone;

    public AttendanceCalculator(TimeZoneInfo timeZone)
    {
      _timeZone = timeZone;
    }

    public void CalculateDailyStatus(DailyLog log, Shift? shift)
    {
      // 1. No shift assigned or no check-in yet: classify as Absent / Present (no shift)
      if (shift == null || !log.CheckIn.HasValue)
      {
        var baseStatus = log.CheckIn.HasValue ? AttendanceStatus.Present : AttendanceStatus.Absent;
        var note = log.CheckIn.HasValue ? "Present (No Shift)" : "Absent";
        log.UpdateCalculationResults(0, 0, 0, 0, baseStatus, note);
        return;
      }

      // 2. Convert stored UTC timestamps to local time (DST-aware via TimeZoneInfo)
      var localCheckIn  = TimeZoneInfo.ConvertTimeFromUtc(log.CheckIn.Value, _timeZone);
      var localCheckOut = log.CheckOut.HasValue
          ? (DateTime?)TimeZoneInfo.ConvertTimeFromUtc(log.CheckOut.Value, _timeZone)
          : null;

      // 3. Build the canonical shift window anchored on the logical work-date (log.Date)
      var shiftStart = log.Date.Add(shift.StartTime);
      var shiftEnd   = log.Date.Add(shift.EndTime);
      if (shift.IsOvernight) shiftEnd = shiftEnd.AddDays(1);

      // 4. LATE detection
      //    Grace period: employee is on-time if CheckIn <= ShiftStart + GracePeriodMinutes.
      //    LateMinutes is measured from the grace-period threshold, NOT from ShiftStart,
      //    so "16 mins past start with 15-min grace" → lateMinutes = 1 (not 16).
      var lateThreshold = shiftStart.AddMinutes(shift.GracePeriodMinutes);
      bool isLate       = localCheckIn > lateThreshold;
      int  lateMinutes  = isLate ? (int)(localCheckIn - lateThreshold).TotalMinutes : 0;

      // 5. EARLY-LEAVE, OVERTIME and WORKING HOURS (require CheckOut)
      bool   isEarlyLeave     = false;
      int    earlyLeaveMinutes = 0;
      double overtimeHours    = 0;
      double workingHours     = 0;

      if (localCheckOut.HasValue)
      {
        // --- Early leave ---
        if (localCheckOut.Value < shiftEnd)
        {
          isEarlyLeave      = true;
          earlyLeaveMinutes = (int)(shiftEnd - localCheckOut.Value).TotalMinutes;
        }
        else
        {
          // --- Overtime ---
          // Only count OT when CheckOut exceeds ShiftEnd by at least OvertimeThresholdMinutes
          var otMinutes = (localCheckOut.Value - shiftEnd).TotalMinutes;
          overtimeHours = otMinutes >= shift.OvertimeThresholdMinutes
              ? Math.Round(otMinutes / 60.0, 2)
              : 0;
        }

        // --- Working hours = total duration minus any overlap with the break window ---
        var duration = (localCheckOut.Value - localCheckIn).TotalHours;

        var breakStart = log.Date.Add(shift.BreakStartTime);
        var breakEnd   = log.Date.Add(shift.BreakEndTime);
        // Handle overnight break (e.g. 23:00 – 00:30)
        if (shift.BreakEndTime < shift.BreakStartTime) breakEnd = breakEnd.AddDays(1);

        var overlapStart = localCheckIn > breakStart ? localCheckIn : breakStart;
        var overlapEnd   = localCheckOut.Value < breakEnd ? localCheckOut.Value : breakEnd;

        double breakDeduct = overlapStart < overlapEnd
            ? (overlapEnd - overlapStart).TotalHours
            : 0;

        workingHours = Math.Max(0, duration - breakDeduct);
      }

      // 6. Persist results — base status is always Present (flags carry the violations)
      log.UpdateCalculationResults(
          workingHours,
          lateMinutes,
          earlyLeaveMinutes,
          overtimeHours,
          AttendanceStatus.Present,
          note: string.Empty,
          isLate: isLate,
          isEarlyLeave: isEarlyLeave);
    }
  }
}