
namespace Employee.Application.Features.Attendance.Dtos
{
  // ==========================================
  // 1. INPUT: CHECK-IN / CHECK-OUT (G?i RawLog)
  // ==========================================
  public class CheckInRequestDto
  {
    // "CheckIn" hoặc "CheckOut"
    public string Type { get; set; } = "CheckIn";

    public string? EmployeeId { get; set; }
    public string DeviceId { get; set; } = "WebApp";

    // Tọa độ GPS (nếu có)
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Ảnh selfie dạng base64 (chụp từ webcam lúc check-in)
    // Format: "data:image/jpeg;base64,/9j/4AAQ..."
    public string? PhotoBase64 { get; set; }
  }

  // ==========================================
  // 2. OUTPUT: MONTHLY ATTENDANCE TABLE (from AttendanceBucket)
  // ==========================================
  public class MonthlyAttendanceDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty; // "02-2026"

    // T?ng h?p nhanh
    public int TotalPresent { get; set; }
    public int TotalLate { get; set; }
    public double TotalWorkingHours { get; set; } // Accumulated working hours

    // Daily log details
    public List<DailyLogDto> Logs { get; set; } = new();
  }

  // DTO for each day's log (mapping from DailyLog value object)
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

    // Base presence status: "Present" | "Absent" | "Leave" | "Holiday"
    public string Status { get; set; } = string.Empty;

    // Granular violation flags (GAP-01 fix: combined violations now representable)
    public bool IsLate { get; set; }
    public bool IsEarlyLeave { get; set; }
    public bool IsMissingPunch { get; set; }

    public double OvertimeHours { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsHoliday { get; set; }
  }
  public class AttendanceRangeDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    // Summary for the requested date range
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

  // ==========================================
  // TODAY STATUS (Check-in / Check-out gating)
  // ==========================================
  public class TodayAttendanceStatusDto
  {
    /// <summary>True if the employee has at least one CheckIn raw log today (local time UTC+7).</summary>
    public bool HasCheckedIn { get; set; }

    /// <summary>True if the employee has at least one CheckOut raw log today (local time UTC+7).</summary>
    public bool HasCheckedOut { get; set; }

    /// <summary>Time of the earliest CheckIn today, as UTC (frontend converts to local).</summary>
    public DateTime? CheckInTime { get; set; }

    /// <summary>Time of the latest CheckOut today, as UTC (frontend converts to local).</summary>
    public DateTime? CheckOutTime { get; set; }
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

    public List<double> DailyHours { get; set; } = new(); // Gi? l
  }
}