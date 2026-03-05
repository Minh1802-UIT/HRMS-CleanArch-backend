using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using Xunit;

namespace Employee.UnitTests.Domain.Entities.Attendance
{
  /// <summary>
  /// Unit tests for AttendanceBucket domain entity.
  /// NOTE: AttendanceStatus.Late / EarlyLeave have been removed from the enum.
  /// Violation tracking now uses IsLate / IsEarlyLeave / IsMissingPunch boolean flags
  /// on DailyLog. All tests updated accordingly.
  /// </summary>
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
      var date   = new DateTime(2026, 2, 1);
      var log    = DailyLog.Create(date, AttendanceStatus.Present);
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
      var date   = new DateTime(2026, 2, 1);

      // First entry: late (30 min) — status=Present with IsLate=true
      var log1 = DailyLog.Create(date, AttendanceStatus.Present);
      log1.UpdateCalculationResults(7.5, 30, 0, 0, AttendanceStatus.Present,
          isLate: true);

      // Corrected entry: on-time, with OT
      var log2 = DailyLog.Create(date, AttendanceStatus.Present);
      log2.UpdateCalculationResults(8.0, 0, 0, 2.0, AttendanceStatus.Present);

      // Act
      bucket.AddOrUpdateDailyLog(log1);
      bucket.AddOrUpdateDailyLog(log2); // should replace log1 for same date

      // Assert
      Assert.Single(bucket.DailyLogs);
      Assert.Equal(1, bucket.TotalPresent);
      Assert.Equal(2.0, bucket.TotalOvertime);
      Assert.Equal(0, bucket.TotalLate);   // log2 has IsLate=false
    }

    [Fact]
    public void RecalculateTotals_ShouldSumCorrectly()
    {
      // Arrange
      var bucket = new AttendanceBucket("emp1", "02-2026");

      // Day 1: Present, on-time, with OT
      var log1 = DailyLog.Create(new DateTime(2026, 2, 1), AttendanceStatus.Present);
      log1.UpdateCalculationResults(8.0, 0, 0, 1.0, AttendanceStatus.Present);

      // Day 2: Present but late — IsLate flag drives TotalLate
      var log2 = DailyLog.Create(new DateTime(2026, 2, 2), AttendanceStatus.Present);
      log2.UpdateCalculationResults(7.5, 30, 0, 0.5, AttendanceStatus.Present,
          isLate: true);

      // Day 3: Absent
      var log3 = DailyLog.Create(new DateTime(2026, 2, 3), AttendanceStatus.Absent);
      log3.UpdateCalculationResults(0, 0, 0, 0, AttendanceStatus.Absent);

      // Act
      bucket.AddOrUpdateDailyLog(log1);
      bucket.AddOrUpdateDailyLog(log2);
      bucket.AddOrUpdateDailyLog(log3);

      // Assert
      Assert.Equal(2, bucket.TotalPresent);   // log1 + log2 are Present
      Assert.Equal(1, bucket.TotalLate);      // only log2 has IsLate=true
      Assert.Equal(1.5, bucket.TotalOvertime);
    }

    [Fact]
    public void RecalculateTotals_LateAndEarlyLeave_BothFlagsCaptured()
    {
      // Arrange: employee was BOTH late AND left early the same day
      var bucket = new AttendanceBucket("emp1", "02-2026");
      var log    = DailyLog.Create(new DateTime(2026, 2, 4), AttendanceStatus.Present);
      log.UpdateCalculationResults(6.0, 20, 45, 0, AttendanceStatus.Present,
          isLate: true, isEarlyLeave: true);

      // Act
      bucket.AddOrUpdateDailyLog(log);

      // Assert — both flags are independently true; TotalPresent=1, TotalLate=1
      Assert.Equal(1, bucket.TotalPresent);
      Assert.Equal(1, bucket.TotalLate);
      Assert.True(bucket.DailyLogs[0].IsLate);
      Assert.True(bucket.DailyLogs[0].IsEarlyLeave);
    }
  }
}
