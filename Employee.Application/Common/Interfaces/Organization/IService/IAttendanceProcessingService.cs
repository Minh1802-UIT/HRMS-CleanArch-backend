namespace Employee.Application.Common.Interfaces.Organization.IService
{
  public interface IAttendanceProcessingService
  {
    Task<string> ProcessRawLogsAsync();

    /// <summary>
    /// Retroactively marks IsHoliday=true on all DailyLogs for the given month
    /// that fall on a public holiday.
    /// Returns the number of attendance buckets updated.
    /// </summary>
    Task<int> BackfillHolidayFlagsAsync(int month, int year);
  }
}
