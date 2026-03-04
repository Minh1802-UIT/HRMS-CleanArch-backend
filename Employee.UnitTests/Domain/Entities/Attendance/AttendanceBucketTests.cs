using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using Xunit;

namespace Employee.UnitTests.Domain.Entities.Attendance
{
  public class AttendanceBucketTests
  {
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
      // Act
      var bucket = new AttendanceBucket("emp123", "02-2026");

      // Assert
      Assert.Equal("emp123", bucket.EmployeeId);
      Assert.Equal("02-2026", bucket.Month);
      Assert.Empty(bucket.DailyLogs);
      Assert.Equal(0, bucket.TotalPresent);
      Assert.Equal(0, bucket.TotalLate);
      Assert.Equal(0, bucket.TotalOvertime);
    }

    [Fact]
    public void AddOrUpdateDailyLog_NewLog_ShouldAddAndRecalculate()
    {
      // Arrange
      var bucket = new AttendanceBucket("emp1", "02-2026");
      var date = new DateTime(2026, 2, 1);
      var log = DailyLog.Create(date, AttendanceStatus.Present);
      // double workingHours, int lateMinutes, int earlyLeaveMinutes, double overtimeHours, AttendanceStatus status
      log.UpdateCalculationResults(8.0, 0, 0, 1.5, AttendanceStatus.Present);

      // Act
      bucket.AddOrUpdateDailyLog(log);

      // Assert
      Assert.Single(bucket.DailyLogs);
      Assert.Equal(1, bucket.TotalPresent);
      Assert.Equal(1.5, bucket.TotalOvertime);
      Assert.Equal(0, bucket.TotalLate);
    }

    [Fact]
    public void AddOrUpdateDailyLog_DuplicateDate_ShouldUpdateAndRecalculate()
    {
      // Arrange
      var bucket = new AttendanceBucket("emp1", "02-2026");
      var date = new DateTime(2026, 2, 1);

      var log1 = DailyLog.Create(date, AttendanceStatus.Late);
      log1.UpdateCalculationResults(7.5, 30, 0, 0, AttendanceStatus.Late); // 30 mins late

      var log2 = DailyLog.Create(date, AttendanceStatus.Present); // Corrected log
      log2.UpdateCalculationResults(8.0, 0, 0, 2.0, AttendanceStatus.Present);

      // Act
      bucket.AddOrUpdateDailyLog(log1);
      bucket.AddOrUpdateDailyLog(log2);

      // Assert
      Assert.Single(bucket.DailyLogs);
      Assert.Equal(1, bucket.TotalPresent);
      Assert.Equal(2.0, bucket.TotalOvertime);
      Assert.Equal(0, bucket.TotalLate);
    }

    [Fact]
    public void RecalculateTotals_ShouldSumCorrectly()
    {
      // Arrange
      var bucket = new AttendanceBucket("emp1", "02-2026");

      // Present
      var log1 = DailyLog.Create(new DateTime(2026, 2, 1), AttendanceStatus.Present);
      log1.UpdateCalculationResults(8.0, 0, 0, 1.0, AttendanceStatus.Present);

      // Late (Counts as present)
      var log2 = DailyLog.Create(new DateTime(2026, 2, 2), AttendanceStatus.Late);
      log2.UpdateCalculationResults(7.5, 30, 0, 0.5, AttendanceStatus.Late);

      // Absent
      var log3 = DailyLog.Create(new DateTime(2026, 2, 3), AttendanceStatus.Absent);
      log3.UpdateCalculationResults(0, 0, 0, 0, AttendanceStatus.Absent);

      // Act
      bucket.AddOrUpdateDailyLog(log1);
      bucket.AddOrUpdateDailyLog(log2);
      bucket.AddOrUpdateDailyLog(log3);

      // Assert
      Assert.Equal(2, bucket.TotalPresent);
      Assert.Equal(1, bucket.TotalLate);
      Assert.Equal(1.5, bucket.TotalOvertime);
    }
  }
}
