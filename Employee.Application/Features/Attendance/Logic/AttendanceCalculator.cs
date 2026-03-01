using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Attendance.Logic
{
  public class AttendanceCalculator
  {
    private readonly TimeZoneInfo _timeZone;

    public AttendanceCalculator(TimeZoneInfo timeZone)
    {
      _timeZone = timeZone;
    }

    /// <summary>
    /// Hàm thuần túy tính toán: Input là Log + Shift -> Output là update trạng thái Log
    /// </summary>
    public void CalculateDailyStatus(DailyLog log, Shift? shift)
    {
      // 1. Nếu không có ca hoặc chưa check-in
      if (shift == null || !log.CheckIn.HasValue)
      {
        var initialStatus = log.CheckIn.HasValue ? AttendanceStatus.Present : AttendanceStatus.Absent;
        string note = log.CheckIn.HasValue ? "Present (No Shift)" : "Absent";

        log.UpdateCalculationResults(0, 0, 0, 0, initialStatus, note);
        return;
      }

      // 2. Chuẩn bị Time (UTC -> Local via TimeZoneInfo — DST-aware)
      var localCheckIn = TimeZoneInfo.ConvertTimeFromUtc(log.CheckIn.Value, _timeZone);
      var localCheckOut = log.CheckOut.HasValue ? (DateTime?)TimeZoneInfo.ConvertTimeFromUtc(log.CheckOut.Value, _timeZone) : null;

      // 3. Xây dựng khung giờ chuẩn của Ca
      var shiftStart = log.Date.Add(shift.StartTime);
      var shiftEnd = log.Date.Add(shift.EndTime);
      if (shift.IsOvernight) shiftEnd = shiftEnd.AddDays(1);

      // 4. Tính LATE
      AttendanceStatus status = AttendanceStatus.Present;
      int lateMinutes = 0;
      var lateThreshold = shiftStart.AddMinutes(shift.GracePeriodMinutes);

      if (localCheckIn > lateThreshold)
      {
        status = AttendanceStatus.Late;
        lateMinutes = (int)(localCheckIn - shiftStart).TotalMinutes;
      }

      // 5. Tính EARLY & WORKING HOURS
      int earlyLeaveMinutes = 0;
      double overtimeHours = 0;
      double workingHours = 0;

      if (localCheckOut.HasValue)
      {
        // Về sớm?
        if (localCheckOut.Value < shiftEnd)
        {
          earlyLeaveMinutes = (int)(shiftEnd - localCheckOut.Value).TotalMinutes;
          if (status != AttendanceStatus.Late) status = AttendanceStatus.EarlyLeave;
        }
        else
        {
          // Tính OT: Nếu về trễ hơn ShiftEnd ít nhất 15 phút thì bắt đầu tính OT
          var otMinutes = (localCheckOut.Value - shiftEnd).TotalMinutes;
          overtimeHours = otMinutes >= 15 ? Math.Round(otMinutes / 60.0, 2) : 0;
        }

        // Tính Working Hours
        var duration = (localCheckOut.Value - localCheckIn).TotalHours;

        // Trừ giờ nghỉ (Logic Overlap)
        var breakStart = log.Date.Add(shift.BreakStartTime);
        var breakEnd = log.Date.Add(shift.BreakEndTime);
        if (shift.BreakEndTime < shift.BreakStartTime) breakEnd = breakEnd.AddDays(1);

        var overlapStart = localCheckIn > breakStart ? localCheckIn : breakStart;
        var overlapEnd = localCheckOut.Value < breakEnd ? localCheckOut.Value : breakEnd;

        double breakDeduct = 0;
        if (overlapStart < overlapEnd)
        {
          breakDeduct = (overlapEnd - overlapStart).TotalHours;
        }

        workingHours = Math.Max(0, duration - breakDeduct);
      }

      log.UpdateCalculationResults(workingHours, lateMinutes, earlyLeaveMinutes, overtimeHours, status);
    }
  }
}