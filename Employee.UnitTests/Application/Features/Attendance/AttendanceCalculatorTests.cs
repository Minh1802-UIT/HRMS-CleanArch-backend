using Employee.Application.Features.Attendance.Logic;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System;

namespace Employee.UnitTests.Application.Features.Attendance
{
  /// <summary>
  /// Tests for <see cref="AttendanceCalculator.CalculateDailyStatus"/>.
  ///
  /// All timestamps in the test are expressed in UTC (the same timezone used by
  /// the calculator's default TimeZoneInfo.Utc instance), so local == UTC and
  /// there is no timezone-conversion noise.
  ///
  /// Shift used across most tests:
  ///   Start  08:00  | End 17:00  | Break 12:00–13:00
  ///   Grace  15 min | OT threshold 15 min | Is NOT overnight
  /// </summary>
  public class AttendanceCalculatorTests
  {
    // ─── Shift factory ────────────────────────────────────────────────────

    /// <summary>Standard 8-17 day shift.</summary>
    private static Shift DayShift(
        int graceMinutes = 15,
        int otThreshold = 15,
        bool overnight = false)
        => new Shift(
            "Day", "S01",
            start: new TimeSpan(8, 0, 0),
            end: new TimeSpan(17, 0, 0),
            breakStart: new TimeSpan(12, 0, 0),
            breakEnd: new TimeSpan(13, 0, 0),
            standardHours: 8.0,
            gracePeriod: graceMinutes,
            isOvernight: overnight,
            overtimeThresholdMinutes: otThreshold);

    /// <summary>
    /// Night shift 22:00 – 06:00 (overnight).
    /// Break at 23:00–23:30 (inside the work window)
    /// so AttendanceCalculator can overlap-deduct it correctly.
    /// </summary>
    private static Shift NightShift()
        => new Shift(
            "Night", "S03",
            start: new TimeSpan(22, 0, 0),
            end: new TimeSpan(6, 0, 0),
            breakStart: new TimeSpan(23, 0, 0),
            breakEnd: new TimeSpan(23, 30, 0),
            standardHours: 7.5,
            gracePeriod: 15,
            isOvernight: true,
            overtimeThresholdMinutes: 15);

    private static AttendanceCalculator UtcCalc()
        => new AttendanceCalculator(TimeZoneInfo.Utc);

    /// <summary>Log date set to a fixed Monday with supplied check-times (UTC).</summary>
    private static DailyLog MakeLog(DateTime? checkIn, DateTime? checkOut)
    {
      var log = DailyLog.Create(new DateTime(2026, 3, 2)); // Monday
      log.UpdateCheckTimes(checkIn, checkOut, "S01");
      return log;
    }

    // ─── No shift assigned ────────────────────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_NoShift_WithCheckIn_ShouldBePresent_NoShift()
    {
      var calc = UtcCalc();
      var log = MakeLog(new DateTime(2026, 3, 2, 8, 5, 0, DateTimeKind.Utc), null);

      calc.CalculateDailyStatus(log, shift: null);

      Assert.Equal(AttendanceStatus.Present, log.Status);
      Assert.Equal("Present (No Shift)", log.Note);
    }

    [Fact]
    public void CalculateDailyStatus_NoShift_NoCheckIn_ShouldBeAbsent()
    {
      var calc = UtcCalc();
      var log = MakeLog(null, null);

      calc.CalculateDailyStatus(log, shift: null);

      Assert.Equal(AttendanceStatus.Absent, log.Status);
    }

    [Fact]
    public void CalculateDailyStatus_ShiftAssigned_NoCheckIn_ShouldBeAbsent()
    {
      var calc = UtcCalc();
      var log = MakeLog(null, null);

      calc.CalculateDailyStatus(log, DayShift());

      Assert.Equal(AttendanceStatus.Absent, log.Status);
    }

    // ─── On time (within grace period) ───────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_CheckInOnTime_ShouldNotBeLate()
    {
      var calc = UtcCalc();
      // Exactly at shift start 08:00 — within 15-min grace
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.False(log.IsLate);
      Assert.Equal(0, log.LateMinutes);
    }

    [Fact]
    public void CalculateDailyStatus_CheckInWithinGrace_ShouldNotBeLate()
    {
      var calc = UtcCalc();
      // 08:14 — 1 minute inside the 15-min grace
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 14, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.False(log.IsLate);
    }

    // ─── Late ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_CheckInAfterGrace_ShouldBeLate()
    {
      var calc = UtcCalc();
      // 08:16 — 1 minute PAST the 15-min grace threshold
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 16, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.True(log.IsLate);
      Assert.Equal(1, log.LateMinutes);
    }

    [Fact]
    public void CalculateDailyStatus_LateMinutes_MeasuredFromGraceThreshold_NotShiftStart()
    {
      var calc = UtcCalc();
      // 08:30 with 15-min grace → late by 15 min (not 30)
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 30, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift(graceMinutes: 15));

      Assert.True(log.IsLate);
      Assert.Equal(15, log.LateMinutes);
    }

    [Fact]
    public void CalculateDailyStatus_ZeroGracePeriod_LateByAnyMinute_ShouldBeLate()
    {
      var calc = UtcCalc();
      // 08:01 — no grace → 1 minute late
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 1, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift(graceMinutes: 0));

      Assert.True(log.IsLate);
      Assert.Equal(1, log.LateMinutes);
    }

    // ─── Early leave ──────────────────────────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_CheckOutBeforeEnd_ShouldBeEarlyLeave()
    {
      var calc = UtcCalc();
      // Checks out at 16:00 — 60 min early
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 16, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.True(log.IsEarlyLeave);
      Assert.Equal(60, log.EarlyLeaveMinutes);
    }

    [Fact]
    public void CalculateDailyStatus_CheckOutAtEnd_ShouldNotBeEarlyLeave()
    {
      var calc = UtcCalc();
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.False(log.IsEarlyLeave);
      Assert.Equal(0, log.EarlyLeaveMinutes);
    }

    // ─── Overtime ─────────────────────────────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_CheckOutBelowOtThreshold_ShouldNotCountOt()
    {
      var calc = UtcCalc();
      // Checks out at 17:14 — only 14 min past end, threshold is 15
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 14, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift(otThreshold: 15));

      Assert.Equal(0.0, log.OvertimeHours);
    }

    [Fact]
    public void CalculateDailyStatus_CheckOutAtOtThreshold_ShouldCountOt()
    {
      var calc = UtcCalc();
      // Checks out at 17:15 — exactly 15 min past end
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 15, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift(otThreshold: 15));

      Assert.True(log.OvertimeHours > 0);
      Assert.Equal(0.25, log.OvertimeHours, precision: 2); // 15/60 = 0.25h
    }

    [Fact]
    public void CalculateDailyStatus_TwoHoursOvertime_ShouldCalculateCorrectly()
    {
      var calc = UtcCalc();
      // Checks out at 19:00 — 2 h past 17:00
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 19, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.Equal(2.0, log.OvertimeHours, precision: 2);
    }

    // ─── Working hours & break deduction ─────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_FullDay_ShouldDeductBreakHour()
    {
      var calc = UtcCalc();
      // 08:00–17:00 with 12:00–13:00 break → 8 h worked
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.Equal(8.0, log.WorkingHours, precision: 2);
    }

    [Fact]
    public void CalculateDailyStatus_CheckOutBeforeBreak_ShouldNotDeductBreak()
    {
      var calc = UtcCalc();
      // 08:00–11:00 — leaves before lunch break starts
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 11, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.Equal(3.0, log.WorkingHours, precision: 2);
    }

    [Fact]
    public void CalculateDailyStatus_CheckInDuringBreak_ShouldDeductPartialBreak()
    {
      var calc = UtcCalc();
      // Arrives at 12:30 (middle of break), leaves at 17:00
      // Overlap = 12:30–13:00 = 0.5 h deducted
      // Duration = 17:00–12:30 = 4.5 h
      // Working = 4.5 - 0.5 = 4.0
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 12, 30, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 17, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.Equal(4.0, log.WorkingHours, precision: 2);
    }

    [Fact]
    public void CalculateDailyStatus_NoCheckOut_ShouldLeaveWorkingHoursZero()
    {
      var calc = UtcCalc();
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 8, 0, 0, DateTimeKind.Utc),
          checkOut: null);

      calc.CalculateDailyStatus(log, DayShift());

      Assert.Equal(0.0, log.WorkingHours);
      Assert.Equal(AttendanceStatus.Present, log.Status);
    }

    // ─── Combined violations ──────────────────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_LateAndEarlyLeave_BothFlagsSet()
    {
      var calc = UtcCalc();
      // Check-in 09:00 (late), check-out 16:00 (early leave)
      var log = MakeLog(
          checkIn: new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc),
          checkOut: new DateTime(2026, 3, 2, 16, 0, 0, DateTimeKind.Utc));

      calc.CalculateDailyStatus(log, DayShift());

      Assert.True(log.IsLate);
      Assert.True(log.IsEarlyLeave);
      Assert.Equal(AttendanceStatus.Present, log.Status);
    }

    // ─── Overnight shift ─────────────────────────────────────────────────

    [Fact]
    public void CalculateDailyStatus_OvernightShift_CheckInOnTime_ShouldNotBeLate()
    {
      var calc = UtcCalc();
      // Log date = 2 Mar; shift 22:00–06:00 next day
      // Check-in at 22:05 — within 15-min grace
      var log = DailyLog.Create(new DateTime(2026, 3, 2));
      log.UpdateCheckTimes(
          new DateTime(2026, 3, 2, 22, 5, 0, DateTimeKind.Utc),
          new DateTime(2026, 3, 3, 6, 0, 0, DateTimeKind.Utc),
          "S03");

      calc.CalculateDailyStatus(log, NightShift());

      Assert.False(log.IsLate);
    }

    [Fact]
    public void CalculateDailyStatus_OvernightShift_WorkingHours_CalculatedCorrectly()
    {
      var calc = UtcCalc();
      // 22:00–06:00 = 8h total − 0.5h break = 7.5h
      var log = DailyLog.Create(new DateTime(2026, 3, 2));
      log.UpdateCheckTimes(
          new DateTime(2026, 3, 2, 22, 0, 0, DateTimeKind.Utc),
          new DateTime(2026, 3, 3, 6, 0, 0, DateTimeKind.Utc),
          "S03");

      calc.CalculateDailyStatus(log, NightShift());

      Assert.Equal(7.5, log.WorkingHours, precision: 2);
    }
  }
}
