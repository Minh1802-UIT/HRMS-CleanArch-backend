
namespace Employee.Application.Features.Attendance.Dtos
{
  // ================== VIEW ==================
  public class ShiftDto
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Tr? v? d?ng string "HH:mm" cho FE d? hi?n th?
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string BreakStartTime { get; set; } = string.Empty;
    public string BreakEndTime { get; set; } = string.Empty;

    public int GracePeriodMinutes { get; set; }
    public bool IsOvernight { get; set; }
    public double StandardWorkingHours { get; set; }
    public bool IsActive { get; set; }
  }

  // ================== CREATE ==================
  public class CreateShiftDto
  {
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Input nh?n vào TimeSpan (FE g?i "08:00:00")
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public TimeSpan BreakStartTime { get; set; }
    public TimeSpan BreakEndTime { get; set; }

    public int GracePeriodMinutes { get; set; } = 15;
    public bool IsOvernight { get; set; } = false;
    public string? Description { get; set; }
  }

  // ================== UPDATE ==================
  public class UpdateShiftDto : CreateShiftDto
  {
    public string Id { get; set; } = string.Empty;
    public bool IsActive { get; set; }
  }

  // ================== LOOKUP ==================
  public class ShiftLookupDto
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty; // "08:00 - 17:00"
  }
}