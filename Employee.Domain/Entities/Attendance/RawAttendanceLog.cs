using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.Attendance
{
    public class RawAttendanceLog : BaseEntity
    {
    public string EmployeeId { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }

    // Type: CheckIn, CheckOut, Biometric
    public RawLogType Type { get; private set; } = RawLogType.Biometric;

    public string DeviceId { get; private set; } = string.Empty;

    // Processing status: True if processed by background job and added to AttendanceBucket
    public bool IsProcessed { get; private set; } = false;

    public string? ProcessingError { get; private set; } // Error note if processing failed

    // Hidden constructor for MongoDB
    private RawAttendanceLog() { }

    public RawAttendanceLog(string employeeId, DateTime timestamp, RawLogType type = RawLogType.Biometric, string deviceId = "")
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");

      EmployeeId = employeeId;
      Timestamp = timestamp;
      Type = type;
      DeviceId = deviceId;
    }

    public void MarkAsProcessed()
    {
      IsProcessed = true;
      ProcessingError = null;
    }

    public void MarkAsFailed(string error)
    {
      ProcessingError = error;
      IsProcessed = false;
    }
    }
}
