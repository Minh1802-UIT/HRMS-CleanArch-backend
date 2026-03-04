using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;

namespace Employee.Domain.Entities.Payroll
{
  /// <summary>
  /// Đại diện một chu kỳ tính lương (Payroll Cycle / Kỳ tính lương).
  ///
  /// Mục đích chính: lưu trữ bất biến (immutable snapshot) các thông số
  /// của chu kỳ lương để đảm bảo rằng dù quy tắc công ty thay đổi sau này,
  /// kết quả lương cũ vẫn chính xác 100%.
  ///
  /// Ví dụ chu kỳ lệch (cutoff 26):
  ///   Month=03, Year=2026 → StartDate=26/02/2026, EndDate=25/03/2026
  ///   StandardWorkingDays = tổng ngày làm việc thực tế trong khoảng đó.
  /// </summary>
  public class PayrollCycle : BaseEntity
  {
    // ── Thông tin chu kỳ ────────────────────────────────────────────────────
    /// <summary>Tháng thanh toán lương (1–12).</summary>
    public int Month { get; private set; }

    /// <summary>Năm thanh toán lương.</summary>
    public int Year { get; private set; }

    /// <summary>
    /// Khóa duy nhất định danh chu kỳ, format "MM-YYYY" (VD: "03-2026").
    /// Dùng để join với <see cref="PayrollEntity.Month"/>.
    /// </summary>
    public string MonthKey { get; private set; } = string.Empty;

    // ── Ngày bắt đầu / kết thúc ────────────────────────────────────────────
    /// <summary>Ngày đầu tiên của chu kỳ chấm công (bao gồm).</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Ngày cuối cùng của chu kỳ chấm công (bao gồm).</summary>
    public DateTime EndDate { get; private set; }

    // ── Mẫu số tính lương (Denominator) ─────────────────────────────────────
    /// <summary>
    /// Tổng số ngày làm việc trong chu kỳ sau khi loại trừ:
    ///   - Ngày cuối tuần (WeeklyDaysOff)
    ///   - Ngày lễ/tết (Public Holidays)
    /// Đây là MẪU SỐ trong công thức: Lương_1_ngày = BaseSalary / StandardWorkingDays
    /// Con số này được tính và KHÓA CỨng khi cycle được tạo — không thay đổi sau đó.
    /// </summary>
    public int StandardWorkingDays { get; private set; }

    // ── Cấu hình snapshot (lưu lại cấu hình tại thời điểm tạo chu kỳ) ───────
    /// <summary>Các ngày nghỉ cuối tuần tại thời điểm tạo chu kỳ (VD: "6,0").</summary>
    public string WeeklyDaysOffSnapshot { get; private set; } = string.Empty;

    /// <summary>Số lượng ngày lễ đã loại trừ khỏi chu kỳ.</summary>
    public int PublicHolidaysExcluded { get; private set; }

    // ── Trạng thái ──────────────────────────────────────────────────────────
    public PayrollCycleStatus Status { get; private set; } = PayrollCycleStatus.Open;

    // ── Constructor cho MongoDB deserialization ──────────────────────────────
    private PayrollCycle() { }

    /// <summary>Tạo mới một chu kỳ lương với đầy đủ thông tin đã tính toán.</summary>
    public PayrollCycle(
        int month,
        int year,
        DateTime startDate,
        DateTime endDate,
        int standardWorkingDays,
        string weeklyDaysOffSnapshot,
        int publicHolidaysExcluded)
    {
      if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
      if (year < 2000 || year > 2100) throw new ArgumentOutOfRangeException(nameof(year));
      if (standardWorkingDays <= 0) throw new ArgumentOutOfRangeException(nameof(standardWorkingDays));
      if (endDate < startDate) throw new ArgumentException("EndDate must be >= StartDate.");

      Month = month;
      Year = year;
      MonthKey = $"{month:D2}-{year}";
      // Đảm bảo lưu dạng DateTimeKind.Utc để MongoDB không áp dụng timezone offset.
      // Nếu để Unspecified, driver C# coi là Local → chuyển sang UTC → bị lệch ngày ở UTC+7.
      StartDate = DateTime.SpecifyKind(startDate.Date, DateTimeKind.Utc);
      EndDate = DateTime.SpecifyKind(endDate.Date, DateTimeKind.Utc);
      StandardWorkingDays = standardWorkingDays;
      WeeklyDaysOffSnapshot = weeklyDaysOffSnapshot;
      PublicHolidaysExcluded = publicHolidaysExcluded;
      Status = PayrollCycleStatus.Open;
      CreatedAt = DateTime.UtcNow;
    }

    // ── Business Methods ─────────────────────────────────────────────────────
    public void MarkProcessing()
    {
      if (Status != PayrollCycleStatus.Open)
        throw new InvalidOperationException($"Cannot move to Processing from {Status}.");
      Status = PayrollCycleStatus.Processing;
    }

    public void Close()
    {
      if (Status == PayrollCycleStatus.Cancelled)
        throw new InvalidOperationException("Cannot close a cancelled cycle.");
      Status = PayrollCycleStatus.Closed;
    }

    public void Cancel()
    {
      if (Status == PayrollCycleStatus.Closed)
        throw new InvalidOperationException("Cannot cancel a closed cycle. Revert individual payrolls first.");
      Status = PayrollCycleStatus.Cancelled;
    }

    /// <summary>
    /// Kiểm tra xem một ngày có nằm trong chu kỳ này không.
    /// Tiện lợi khi validate attendance records.
    /// </summary>
    public bool ContainsDate(DateTime date) =>
        date.Date >= StartDate && date.Date <= EndDate;
  }
}
