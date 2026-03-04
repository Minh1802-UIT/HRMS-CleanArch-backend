using Employee.Domain.Entities.Payroll;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IPublicHolidayRepository : IBaseRepository<PublicHoliday>
  {
    /// <summary>
    /// Lấy tất cả ngày lễ trong một khoảng thời gian (bao gồm cả hai đầu).
    /// Kết hợp cả ngày lễ cố định theo năm và ngày lễ một lần.
    /// </summary>
    Task<IEnumerable<PublicHoliday>> GetByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả ngày lễ áp dụng cho một năm cụ thể
    /// (IsRecurringYearly=true hoặc năm của Date trùng với <paramref name="year"/>).
    /// </summary>
    Task<IEnumerable<PublicHoliday>> GetByYearAsync(
        int year,
        CancellationToken cancellationToken = default);
  }
}
