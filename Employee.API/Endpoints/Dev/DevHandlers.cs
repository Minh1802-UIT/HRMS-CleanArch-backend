using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace Employee.API.Endpoints.Dev
{
  /// <summary>
  /// DEV-ONLY endpoints for seeding test data. Only registered in Development environment.
  /// </summary>
  public static class DevHandlers
  {
    /// <summary>
    /// Seeds attendance buckets for all active employees for a given month (default: previous month).
    /// Skips employees that already have a bucket for that month.
    /// All employees receive full Mon-Fri presence.
    /// </summary>
    public static async Task<IResult> SeedAttendance(
        [FromQuery] string? month,
        IEmployeeRepository empRepo,
        IAttendanceRepository attendanceRepo,
        IHostEnvironment env)
    {
      if (!env.IsDevelopment())
        return Results.StatusCode(403);

      var targetMonth = string.IsNullOrWhiteSpace(month)
          ? DateTime.UtcNow.AddMonths(-1).ToString("MM-yyyy")
          : month;

      // Parse month
      if (!DateTime.TryParseExact(targetMonth, "MM-yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var firstDay))
      {
        return Results.BadRequest($"Invalid month format '{targetMonth}'. Expected MM-yyyy.");
      }

      // Collect all Mon-Fri working days in the month
      var workingDays = new List<DateTime>();
      for (int d = 1; d <= DateTime.DaysInMonth(firstDay.Year, firstDay.Month); d++)
      {
        var date = new DateTime(firstDay.Year, firstDay.Month, d, 0, 0, 0, DateTimeKind.Utc);
        if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
          workingDays.Add(date);
      }

      // Find employee IDs that already have a bucket this month (to skip)
      var existing = await attendanceRepo.GetByMonthAsync(targetMonth);
      var existingIds = new HashSet<string>(existing.Select(b => b.EmployeeId));

      // Get all active employees
      var employees = await empRepo.GetAllActiveAsync();
      int created = 0;
      int skipped = 0;

      foreach (var emp in employees)
      {
        if (existingIds.Contains(emp.Id))
        {
          skipped++;
          continue;
        }

        var bucket = new AttendanceBucket(emp.Id, targetMonth);

        foreach (var day in workingDays)
        {
          var log = DailyLog.Create(day, AttendanceStatus.Present);
          // CheckIn 08:00 ICT = 01:00 UTC; CheckOut 17:00 ICT = 10:00 UTC
          log.UpdateCheckTimes(day.AddHours(1), day.AddHours(10), "S01");
          log.UpdateCalculationResults(8.0, 0, 0, 0, AttendanceStatus.Present);
          bucket.AddOrUpdateDailyLog(log);
        }

        await attendanceRepo.CreateAsync(bucket);
        created++;
      }

      return Results.Ok(new
      {
        month = targetMonth,
        workingDaysInMonth = workingDays.Count,
        bucketsCreated = created,
        bucketsSkipped = skipped,
        message = $"Seeded {created} new attendance buckets for {targetMonth} ({workingDays.Count} working days each). Skipped {skipped} (already existed)."
      });
    }
  }
}
