using Employee.Domain.Entities.Common;

namespace Employee.Domain.Entities.Attendance
{
  /// <summary>
  /// Admin-approved overtime entry for a specific employee on a specific date.
  /// Only days that have an OvertimeSchedule record will have OT hours calculated.
  /// All other checkout-after-shift-end minutes are discarded.
  /// </summary>
  public class OvertimeSchedule : BaseEntity
  {
    public string EmployeeId { get; private set; } = string.Empty;

    /// <summary>The calendar date for which OT is approved (UTC midnight, date-only).</summary>
    public DateTime Date { get; private set; }

    /// <summary>Optional note from admin (e.g. "Client deadline", "Month-end closing").</summary>
    public string? Note { get; private set; }

    // Private ctor for MongoDB deserialization
    private OvertimeSchedule() { }

    public OvertimeSchedule(string employeeId, DateTime date, string? note)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");

      EmployeeId = employeeId;
      Date = date.Date; // normalise to date-only
      Note = note?.Trim();
      CreatedAt = DateTime.UtcNow;
    }

    public void UpdateNote(string? note) => Note = note?.Trim();
  }
}
