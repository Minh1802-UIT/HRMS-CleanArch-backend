namespace Employee.Application.Features.Attendance.Dtos
{
  public class CreateOvertimeScheduleDto
  {
    /// <summary>Target employee (required).</summary>
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>The date for which OT is approved.</summary>
    public DateTime Date { get; set; }

    /// <summary>Admin comment, e.g. "Client deadline", "Month-end closing".</summary>
    public string? Note { get; set; }
  }

  /// <summary>Bulk create: same note, multiple dates for one employee.</summary>
  public class CreateBulkOvertimeScheduleDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public List<DateTime> Dates { get; set; } = new();
    public string? Note { get; set; }
  }

  public class OvertimeScheduleDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
