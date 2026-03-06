namespace Employee.Application.Features.Attendance.Dtos
{
  // ── INPUT ──────────────────────────────────────────────────────────────────

  /// <summary>Nhân viên gửi giải trình cho ngày quên check-out.</summary>
  public class SubmitExplanationDto
  {
    public DateTime WorkDate { get; set; }
    public string Reason { get; set; } = string.Empty;
  }

  /// <summary>Manager / HR duyệt hoặc từ chối giải trình.</summary>
  public class ReviewExplanationDto
  {
    /// <summary>"Approve" | "Reject"</summary>
    public string Action { get; set; } = string.Empty;
    public string? Note { get; set; }
  }

  // ── OUTPUT ─────────────────────────────────────────────────────────────────

  public class AttendanceExplanationDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public DateTime WorkDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;   // "Pending" | "Approved" | "Rejected"
    public string? ReviewedBy { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime CreatedAt { get; set; }
  }
}
