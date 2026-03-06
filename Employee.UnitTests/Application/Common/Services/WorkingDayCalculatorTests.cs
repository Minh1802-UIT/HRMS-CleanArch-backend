using Employee.Application.Common.Services;
using System;
using System.Collections.Generic;

namespace Employee.UnitTests.Application.Common.Services
{
  public class WorkingDayCalculatorTests
  {
    private static readonly WorkingDayCalculator Calc = new();

    // Standard weekend config (Sat+Sun off, no holidays)
    private static readonly IReadOnlyList<DayOfWeek> WeekendOff =
        new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };

    private static readonly IReadOnlyList<DateTime> NoHolidays =
        Array.Empty<DateTime>();

    // ─── Edge cases ───────────────────────────────────────────────────────

    [Fact]
    public void Calculate_EndBeforeStart_ShouldReturnZero()
    {
      var result = Calc.Calculate(
          new DateTime(2026, 3, 10),
          new DateTime(2026, 3, 9),
          WeekendOff, NoHolidays);

      Assert.Equal(0, result);
    }

    [Fact]
    public void Calculate_SameDayWeekday_ShouldReturnOne()
    {
      // Monday 2 March 2026
      var monday = new DateTime(2026, 3, 2);
      var result = Calc.Calculate(monday, monday, WeekendOff, NoHolidays);

      Assert.Equal(1, result);
    }

    [Fact]
    public void Calculate_SameDaySaturday_ShouldReturnZero()
    {
      // Saturday 7 March 2026
      var saturday = new DateTime(2026, 3, 7);
      var result = Calc.Calculate(saturday, saturday, WeekendOff, NoHolidays);

      Assert.Equal(0, result);
    }

    // ─── Full weeks ───────────────────────────────────────────────────────

    [Fact]
    public void Calculate_FullCalendarWeek_ShouldReturnFive()
    {
      // Mon–Sun 2–8 March 2026
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),
          new DateTime(2026, 3, 8),
          WeekendOff, NoHolidays);

      Assert.Equal(5, result);
    }

    [Fact]
    public void Calculate_TwoFullWeeks_ShouldReturnTen()
    {
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),
          new DateTime(2026, 3, 15),
          WeekendOff, NoHolidays);

      Assert.Equal(10, result);
    }

    // ─── Holidays ─────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_OneHolidayOnWeekday_ShouldDeductOne()
    {
      // Tue 3 March 2026 is a holiday
      var holiday = new DateTime(2026, 3, 3);
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),
          new DateTime(2026, 3, 8),
          WeekendOff, new[] { holiday });

      // 5 working days - 1 holiday = 4
      Assert.Equal(4, result);
    }

    [Fact]
    public void Calculate_HolidayOnWeekend_ShouldNotDeductAnything()
    {
      // Saturday 7 March 2026 is a public holiday — already off
      var holiday = new DateTime(2026, 3, 7);
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),
          new DateTime(2026, 3, 8),
          WeekendOff, new[] { holiday });

      Assert.Equal(5, result);
    }

    [Fact]
    public void Calculate_HolidayWithTimeComponent_ShouldStillBeRecognized()
    {
      // Holiday stored with noon time — should still match the date
      var holiday = new DateTime(2026, 3, 3, 12, 0, 0);
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),
          new DateTime(2026, 3, 8),
          WeekendOff, new[] { holiday });

      Assert.Equal(4, result);
    }

    [Fact]
    public void Calculate_MultipleHolidays_ShouldDeductCorrectly()
    {
      var holidays = new[]
      {
        new DateTime(2026, 3, 2), // Mon
        new DateTime(2026, 3, 4), // Wed
      };
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),
          new DateTime(2026, 3, 8),
          WeekendOff, holidays);

      // 5 - 2 = 3
      Assert.Equal(3, result);
    }

    // ─── Custom weeklyDaysOff ─────────────────────────────────────────────

    [Fact]
    public void Calculate_OnlySundayOff_ShouldCountSixDaysPerWeek()
    {
      var sundayOnly = new[] { DayOfWeek.Sunday };
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),   // Mon
          new DateTime(2026, 3, 8),   // Sun (off)
          sundayOnly, NoHolidays);

      // Mon–Sat = 6 days
      Assert.Equal(6, result);
    }

    [Fact]
    public void Calculate_NoDaysOff_ShouldCountAllSevenDays()
    {
      var result = Calc.Calculate(
          new DateTime(2026, 3, 2),
          new DateTime(2026, 3, 8),
          Array.Empty<DayOfWeek>(), NoHolidays);

      Assert.Equal(7, result);
    }

    // ─── Cross-month & cross-year ─────────────────────────────────────────

    [Fact]
    public void Calculate_CrossMonthBoundary_ShouldCountCorrectly()
    {
      // 25 Feb – 6 Mar 2026 (Mon–Fri each week)
      var result = Calc.Calculate(
          new DateTime(2026, 2, 23),
          new DateTime(2026, 3, 6),
          WeekendOff, NoHolidays);

      // Weeks: 23Feb(Mon)–27Feb(Fri) = 5,  2Mar(Mon)–6Mar(Fri) = 5  → 10
      Assert.Equal(10, result);
    }
  }
}
