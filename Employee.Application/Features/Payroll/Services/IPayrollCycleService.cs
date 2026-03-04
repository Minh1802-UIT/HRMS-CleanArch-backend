using Employee.Domain.Entities.Payroll;

namespace Employee.Application.Features.Payroll.Services
{
  public interface IPayrollCycleService
  {
    /// <summary>
    /// Tạo (hoặc lấy nếu đã tồn tại) chu kỳ lương cho tháng/năm được chỉ định.
    ///
    /// Logic:
    ///   - Nếu chu kỳ đã tồn tại → trả về chu kỳ đó (idempotent, an toàn khi gọi nhiều lần).
    ///   - Nếu chưa tồn tại → tính StartDate, EndDate, StandardWorkingDays → tạo mới và lưu DB.
    ///
    /// Công thức chu kỳ lệch (PAYROLL_START_DAY=26, PAYROLL_END_DAY=25):
    ///   Tháng 03/2026 → StartDate=26/02/2026, EndDate=25/03/2026
    /// </summary>
    Task<PayrollCycle> GeneratePayrollCycleAsync(int month, int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chu kỳ lương theo MonthKey (VD: "03-2026").
    /// Throws <see cref="KeyNotFoundException"/> nếu chưa tạo.
    /// </summary>
    Task<PayrollCycle> GetCycleAsync(string monthKey, CancellationToken cancellationToken = default);

    /// <summary>Lấy tất cả chu kỳ trong một năm.</summary>
    Task<IEnumerable<PayrollCycle>> GetCyclesByYearAsync(int year, CancellationToken cancellationToken = default);
  }
}
