using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.Attendance.Dtos
{
  // ==========================================
  // 1. INPUT: CHECK-IN / CHECK-OUT (Gửi RawLog)
  // ==========================================
  public class CheckInRequestDto
  {
    // "CheckIn" hoặc "CheckOut"
    [Required]
    [RegularExpression("^(CheckIn|CheckOut)$")]
    public string Type { get; set; } = "CheckIn";

    public string? EmployeeId { get; set; }
    public string DeviceId { get; set; } = "MobileApp";

    // Tọa độ (nếu có)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
  }

  // ==========================================
  // 2. OUTPUT: BẢNG CÔNG THÁNG (Từ AttendanceBucket)
  // ==========================================
  public class MonthlyAttendanceDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty; // "02-2026"

    // Tổng hợp nhanh
    public int TotalPresent { get; set; }
    public int TotalLate { get; set; }
    public double TotalWorkingHours { get; set; } // Cộng dồn giờ làm

    // Chi tiết từng ngày
    public List<DailyLogDto> Logs { get; set; } = new();
  }

  // DTO chi tiết cho từng ngày (Mapping từ ValueObject DailyLog)
  public class DailyLogDto
  {
    public DateTime Date { get; set; }
    public string DayOfWeek { get; set; } = string.Empty; // "Mon", "Tue"...

    public DateTime? CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }

    public string ShiftCode { get; set; } = string.Empty;
    public double WorkingHours { get; set; }

    public int LateMinutes { get; set; }
    public int EarlyLeaveMinutes { get; set; }

    public string Status { get; set; } = string.Empty; // Present, Absent, Late...
    public double OvertimeHours { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsHoliday { get; set; }
  }
  public class AttendanceRangeDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // Tổng hợp trong khoảng thời gian này
    public double TotalWorkingHours { get; set; }
    public double TotalOvertimeHours { get; set; }

    public List<DailyLogDto> Logs { get; set; } = new();
  }

  public class TeamAttendanceSummaryDto
  {
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<TeamMemberAttendanceDto> Members { get; set; } = new();
  }

  public class TeamMemberAttendanceDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string EmployeeType { get; set; } = string.Empty; // Full time, etc.
    public string Department { get; set; } = string.Empty;
    public string Office { get; set; } = string.Empty;

    public double TotalWorkedHours { get; set; }
    public double ExpectedHours { get; set; }
    public double Overtime { get; set; }
    public string Status { get; set; } = "Pending"; // Approved, Rejected, Pending

    public List<double> DailyHours { get; set; } = new(); // Giờ làm từng ngày để vẽ chart/table
  }
}