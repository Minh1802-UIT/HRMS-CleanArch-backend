using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;

namespace Employee.Domain.Entities.Attendance
{
  /// <summary>
  /// Giải trình chấm công: nhân viên tạo khi quên check-out (IsMissingPunch).
  /// Manager approve → hệ thống tự cập nhật ngày đó 8 tiếng đủ công.
  /// Manager reject → ngày đó giữ nguyên 0 giờ (mất 1 công).
  /// </summary>
  public class AttendanceExplanation : BaseEntity
  {
    public string EmployeeId { get; private set; } = string.Empty;

    // Ngày công cần giải trình (chỉ lấy phần Date)
    public DateTime WorkDate { get; private set; }

    // Lý do giải trình của nhân viên
    public string Reason { get; private set; } = string.Empty;

    public ExplanationStatus Status { get; private set; } = ExplanationStatus.Pending;

    // Người duyệt (ManagerId / HR userId)
    public string? ReviewedBy { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    // Private ctor for MongoDB deserialization
    private AttendanceExplanation() { }

    public AttendanceExplanation(string employeeId, DateTime workDate, string reason)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Reason is required.");
      if (reason.Length > 500) throw new ArgumentException("Reason must not exceed 500 characters.");

      EmployeeId = employeeId;
      WorkDate = workDate.Date;   // normalise to date-only
      Reason = reason;
      Status = ExplanationStatus.Pending;
      CreatedAt = DateTime.UtcNow;
    }

    public void Approve(string reviewedBy, string? note)
    {
      if (Status != ExplanationStatus.Pending)
        throw new InvalidOperationException($"Cannot approve explanation in '{Status}' status.");

      Status = ExplanationStatus.Approved;
      ReviewedBy = reviewedBy;
      ReviewNote = note;
      ReviewedAt = DateTime.UtcNow;
    }

    public void Reject(string reviewedBy, string note)
    {
      if (Status != ExplanationStatus.Pending)
        throw new InvalidOperationException($"Cannot reject explanation in '{Status}' status.");
      if (string.IsNullOrWhiteSpace(note))
        throw new ArgumentException("Reject note is required.");

      Status = ExplanationStatus.Rejected;
      ReviewedBy = reviewedBy;
      ReviewNote = note;
      ReviewedAt = DateTime.UtcNow;
    }
  }
}
