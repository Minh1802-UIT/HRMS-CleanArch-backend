namespace Employee.Application.Common.Interfaces
{
  /// <summary>
  /// Tính số ngày làm việc thực tế trong một chu kỳ lương, loại trừ:
  /// - Các ngày nghỉ cuối tuần (<see cref="weeklyDaysOff"/>)
  /// - Các ngày lễ/tết (<see cref="publicHolidays"/>)
  /// </summary>
  public interface IWorkingDayCalculator
  {
    /// <summary>
    /// Đếm số ngày làm việc trong [startDate, endDate] (bao gồm hai đầu).
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu chu kỳ (bao gồm).</param>
    /// <param name="endDate">Ngày kết thúc chu kỳ (bao gồm).</param>
    /// <param name="weeklyDaysOff">Các ngày nghỉ cố định trong tuần, VD: Saturday, Sunday.</param>
    /// <param name="publicHolidays">Danh sách ngày lễ/tết cần loại trừ.</param>
    /// <returns>Số ngày làm việc (mẫu số tính lương).</returns>
    int Calculate(
        DateTime startDate,
        DateTime endDate,
        IReadOnlyList<DayOfWeek> weeklyDaysOff,
        IReadOnlyList<DateTime> publicHolidays);
  }
}
