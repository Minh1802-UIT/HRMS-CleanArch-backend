using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.Attendance
{
    public class RawAttendanceLog : BaseEntity
    {
    public string EmployeeId { get; internal set; } = string.Empty;
    public DateTime Timestamp { get; internal set; }

    // Type: CheckIn, CheckOut, Biometric
    public RawLogType Type { get; internal set; } = RawLogType.Biometric;

    public string DeviceId { get; internal set; } = string.Empty;

    // Processing status: True if processed by background job and added to AttendanceBucket
    public bool IsProcessed { get; internal set; } = false;

    public string? ProcessingError { get; internal set; } // Error note if processing failed

    // Optional: base64 selfie photo captured during check-in
    public string? PhotoBase64 { get; internal set; }

    // GPS coordinates captured on device
    public double? Latitude { get; internal set; }
    public double? Longitude { get; internal set; }

    // Hidden constructor for MongoDB
    private RawAttendanceLog() { }

    public RawAttendanceLog(
      string employeeId,
      DateTime timestamp,
      RawLogType type = RawLogType.Biometric,
      string deviceId = "",
      string? photoBase64 = null,
      double? latitude = null,
      double? longitude = null)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");

      EmployeeId = employeeId;
      Timestamp = timestamp;
      Type = type;
      DeviceId = deviceId;
      PhotoBase64 = photoBase64;
      Latitude = latitude;
      Longitude = longitude;
      CreatedAt = DateTime.UtcNow;
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
