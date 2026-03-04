using Employee.Domain.Entities.Payroll;
using Employee.Domain.Enums;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface IPayrollCycleRepository : IBaseRepository<PayrollCycle>
  {
    /// <summary>
    /// Tìm chu kỳ lương theo MonthKey (VD: "03-2026").
    /// Trả null nếu chưa tồn tại — caller quyết định có tạo mới không.
    /// </summary>
    Task<PayrollCycle?> GetByMonthKeyAsync(string monthKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách chu kỳ lương theo năm.
    /// </summary>
    Task<IEnumerable<PayrollCycle>> GetByYearAsync(int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra chu kỳ đã tồn tại chưa (không lấy full document — nhẹ hơn).
    /// </summary>
    Task<bool> ExistsAsync(string monthKey, CancellationToken cancellationToken = default);
  }
}
