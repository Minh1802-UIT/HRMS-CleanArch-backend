using System;
using Xunit;
using Employee.Application.Common.Utils;

namespace Employee.UnitTests.Application.Common.Utils
{
  public class DateHelperTests
  {
    // =====================================================================
    //  CountWorkingDays — existing behaviour (regression guard)
    // =====================================================================

    [Theory]
    [InlineData("2026-02-23", "2026-02-23", 1)] // Monday
    [InlineData("2026-02-23", "2026-02-27", 5)] // Mon–Fri
    [InlineData("2026-02-21", "2026-02-22", 0)] // Sat–Sun (both weekends)
    [InlineData("2026-02-20", "2026-02-23", 2)] // Fri–Mon = Fri + Mon = 2 working days
    [InlineData("2026-02-09", "2026-02-13", 5)] // Mon–Fri across full week
    public void CountWorkingDays_Returns_CorrectWorkingDays(string start, string end, double expected)
    {
      var from = DateTime.Parse(start);
      var to = DateTime.Parse(end);

      var result = DateHelper.CountWorkingDays(from, to);

      Assert.Equal(expected, result);
    }

    [Fact]
    public void CountWorkingDays_SameDay_Weekend_Returns_Zero()
    {
      // Saturday
      var result = DateHelper.CountWorkingDays(new DateTime(2026, 2, 21), new DateTime(2026, 2, 21));
      Assert.Equal(0, result);
    }

    // =====================================================================
    //  CountCalendarDays — Sandwich Rule
    // =====================================================================

    [Theory]
    [InlineData("2026-02-23", "2026-02-23", 1)] // Single Monday
    [InlineData("2026-02-20", "2026-02-23", 4)] // Fri-Mon = 4 calendar days (sandwich rule catches Sat+Sun)
    [InlineData("2026-02-23", "2026-02-27", 5)] // Mon-Fri = 5 (same as working days, no weekend inside)
    [InlineData("2026-02-21", "2026-02-22", 2)] // Sat-Sun = 2 calendar days
    [InlineData("2026-02-16", "2026-02-23", 8)] // Mon to Mon = 8 calendar days
    public void CountCalendarDays_Returns_AllDaysIncludingWeekends(string start, string end, double expected)
    {
      var from = DateTime.Parse(start);
      var to = DateTime.Parse(end);

      var result = DateHelper.CountCalendarDays(from, to);

      Assert.Equal(expected, result);
    }

    [Fact]
    public void CountCalendarDays_FridayToMonday_Is4_NotWorkingDays2()
    {
      // This is the canonical sandwich rule case:
      // Taking Fri + Mon = 2 working days without sandwich rule,
      // BUT 4 calendar days WITH sandwich rule.
      var friday = new DateTime(2026, 2, 20);
      var monday = new DateTime(2026, 2, 23);

      var calendar = DateHelper.CountCalendarDays(friday, monday);
      var working = DateHelper.CountWorkingDays(friday, monday);

      Assert.Equal(4, calendar);   // Fri, Sat, Sun, Mon
      Assert.Equal(2, working);    // Only Fri and Mon
      Assert.True(calendar > working, "Sandwich rule should count more days than working-days count.");
    }

    [Fact]
    public void CountCalendarDays_IsAlwaysGteCountWorkingDays()
    {
      // For any date range, calendar days >= working days
      var start = new DateTime(2026, 1, 1);
      for (int i = 0; i < 30; i++)
      {
        var end = start.AddDays(i);
        var calendar = DateHelper.CountCalendarDays(start, end);
        var working = DateHelper.CountWorkingDays(start, end);
        Assert.True(calendar >= working, $"Calendar ({calendar}) must be >= working ({working}) for range {start:d} to {end:d}");
      }
    }
  }
}
