using Employee.Application.Common.Interfaces;
using Employee.Infrastructure.Persistence;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Payroll;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Payroll
{
  public class PublicHolidayRepository : BaseRepository<PublicHoliday>, IPublicHolidayRepository
  {
    public PublicHolidayRepository(IMongoContext context) : base(context, "public_holidays")
    {
    }

    /// <summary>
    /// Lấy các ngày lễ trong phạm vi [startDate, endDate].
    /// Bao gồm:
    /// - Ngày lễ có IsRecurringYearly=true VÀ tháng/ngày nằm trong chu kỳ (bất kể năm).
    /// - Ngày lễ một lần (IsRecurringYearly=false) có Date nằm trong chu kỳ.
    /// </summary>
    public async Task<IEnumerable<PublicHoliday>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
      // Lấy tất cả ngày lễ active (soft-delete filter tự động qua SoftDeleteFilter)
      var filter = SoftDeleteFilter.GetActiveOnlyFilter<PublicHoliday>();
      var all = await _collection.Find(filter).ToListAsync(cancellationToken);

      // Lọc phía ứng dụng để xử lý logic IsRecurringYearly
      return all.Where(h => IsHolidayInRange(h, startDate, endDate));
    }

    /// <summary>
    /// Lấy tất cả ngày lễ áp dụng cho năm <paramref name="year"/>.
    /// </summary>
    public async Task<IEnumerable<PublicHoliday>> GetByYearAsync(
        int year,
        CancellationToken cancellationToken = default)
    {
      var startOfYear = new DateTime(year, 1, 1);
      var endOfYear = new DateTime(year, 12, 31);
      return await GetByDateRangeAsync(startOfYear, endOfYear, cancellationToken);
    }

    // ─── Helper ──────────────────────────────────────────────────────────────

    private static bool IsHolidayInRange(PublicHoliday holiday, DateTime startDate, DateTime endDate)
    {
      if (holiday.IsRecurringYearly)
      {
        // Với ngày lễ lặp lại hàng năm, kiểm tra từng năm trong chu kỳ
        for (int y = startDate.Year; y <= endDate.Year; y++)
        {
          // Bỏ qua nếu tháng/ngày không hợp lệ cho năm đó (VD: 29/2 năm không nhuận)
          if (!IsValidDayInYear(holiday.Date.Month, holiday.Date.Day, y)) continue;
          var occurrenceThisYear = new DateTime(y, holiday.Date.Month, holiday.Date.Day);
          if (occurrenceThisYear >= startDate.Date && occurrenceThisYear <= endDate.Date)
            return true;
        }
        return false;
      }
      else
      {
        // Ngày lễ một lần — kiểm tra Date trực tiếp
        return holiday.Date >= startDate.Date && holiday.Date <= endDate.Date;
      }
    }

    private static bool IsValidDayInYear(int month, int day, int year)
    {
      try { _ = new DateTime(year, month, day); return true; }
      catch { return false; }
    }
  }
}
