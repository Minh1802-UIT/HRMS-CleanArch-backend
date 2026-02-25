using Employee.Application.Features.Attendance.Dtos;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System;
using System.Linq;

namespace Employee.Application.Features.Attendance.Mappers
{
  public static class AttendanceMapper
  {
    // 1. RawLog Mapper (Dùng khi Check-in)
    public static RawAttendanceLog ToRawEntity(this CheckInRequestDto dto, string employeeId)
    {
      var type = dto.Type == "CheckIn" ? RawLogType.CheckIn : RawLogType.CheckOut;

      return new RawAttendanceLog(
        employeeId,
        DateTime.UtcNow,
        type,
        dto.DeviceId
      );
    }

    // 2. Bucket Mapper (Dùng khi xem bảng công tháng)
    public static MonthlyAttendanceDto ToDto(this AttendanceBucket entity)
    {
      // Tính tổng giờ làm của cả tháng
      double totalHours = entity.DailyLogs.Sum(x => x.WorkingHours);

      return new MonthlyAttendanceDto
      {
        EmployeeId = entity.EmployeeId,
        Month = entity.Month,
        TotalPresent = entity.TotalPresent,
        TotalLate = entity.TotalLate,
        TotalWorkingHours = Math.Round(totalHours, 2),

        // Map danh sách ngày công
        Logs = entity.DailyLogs.Select(log => log.ToDto()).OrderBy(x => x.Date).ToList()
      };
    }

    // 3. DailyLog Mapper (Helper function)
    public static DailyLogDto ToDto(this DailyLog log)
    {
      return new DailyLogDto
      {
        Date = log.Date,
        DayOfWeek = log.Date.DayOfWeek.ToString(),
        CheckInTime = log.CheckIn,
        CheckOutTime = log.CheckOut,
        ShiftCode = log.ShiftCode,
        WorkingHours = Math.Round(log.WorkingHours, 2),
        LateMinutes = log.LateMinutes,
        EarlyLeaveMinutes = log.EarlyLeaveMinutes,
        Status = log.Status.ToString(),
        OvertimeHours = Math.Round(log.OvertimeHours, 2),
        IsWeekend = log.IsWeekend,
        IsHoliday = log.IsHoliday
      };
    }
  }
}