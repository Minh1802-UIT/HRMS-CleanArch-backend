using Employee.Application.Common.Interfaces;

namespace Employee.Application.Common.Services
{
  /// <summary>
  /// Tính số ngày làm việc thực tế trong chu kỳ lương.
  /// Không có I/O — chỉ là phép tính thuần tuý trên ngày tháng.
  /// </summary>
  public class WorkingDayCalculator : IWorkingDayCalculator
  {
    /// <inheritdoc/>
    public int Calculate(
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<DayOfWeek> weeklyDaysOff,
        IReadOnlyList<DateTime> publicHolidays)
    {
      if (endDate < startDate) return 0;

      // Chuẩn hóa về Date (bỏ phần time) để so sánh chính xác
      var holidaySet = publicHolidays
          .Select(h => h.Date)
          .ToHashSet();

      int count = 0;
      for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
      {
        if (!weeklyDaysOff.Contains(date.DayOfWeek) && !holidaySet.Contains(date))
          count++;
      }
      return count;
    }
  }
}
