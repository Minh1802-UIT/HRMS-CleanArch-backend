using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Employee.Application.Features.Payroll.Services
{
  /// <summary>
  /// Triển khai logic tạo chu kỳ lương với "Shifted Payroll Cycle" (Chu kỳ lệch)
  /// và tính toán "Dynamic Standard Working Days" (Mẫu số ngày công động).
  ///
  /// ═══════════════════════════════════════════════════════
  /// THUẬT TOÁN TÍNH CHU KỲ LƯƠNG (PAYROLL CYCLE ALGORITHM)
  /// ═══════════════════════════════════════════════════════
  ///
  /// Cấu hình: PAYROLL_START_DAY = 26, PAYROLL_END_DAY = 25
  ///
  ///   Tháng T/Y → StartDate = ngày 26 của tháng (T-1)/(Y-1)
  ///             → EndDate   = ngày 25 của tháng T/Y
  ///
  ///   VD: Tháng 03/2026 → StartDate = 26/02/2026, EndDate = 25/03/2026
  ///   VD: Tháng 01/2026 → StartDate = 26/12/2025, EndDate = 25/01/2026
  ///
  /// Mẫu số (StandardWorkingDays) = tổng ngày trong [StartDate, EndDate]
  ///   trừ ngày cuối tuần (T7, CN) và ngày lễ/tết.
  ///
  /// Công thức lương:
  ///   DailyWage     = BaseSalary / StandardWorkingDays
  ///   FinalSalary   = DailyWage × ActualWorkingDays + OvertimePay
  /// ═══════════════════════════════════════════════════════
  /// </summary>
  public class PayrollCycleService : IPayrollCycleService
  {
    private readonly IPayrollCycleRepository _cycleRepo;
    private readonly IPublicHolidayRepository _holidayRepo;
    private readonly ISystemSettingService _settingService;
    private readonly IWorkingDayCalculator _workingDayCalculator;
    private readonly ILogger<PayrollCycleService> _logger;

    public PayrollCycleService(
        IPayrollCycleRepository cycleRepo,
        IPublicHolidayRepository holidayRepo,
        ISystemSettingService settingService,
        IWorkingDayCalculator workingDayCalculator,
        ILogger<PayrollCycleService> logger)
    {
      _cycleRepo = cycleRepo;
      _holidayRepo = holidayRepo;
      _settingService = settingService;
      _workingDayCalculator = workingDayCalculator;
      _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<PayrollCycle> GeneratePayrollCycleAsync(
        int month, int year, CancellationToken cancellationToken = default)
    {
      var monthKey = $"{month:D2}-{year}";

      // 1. Idempotency check — trả về chu kỳ đã tồn tại nếu có
      var existing = await _cycleRepo.GetByMonthKeyAsync(monthKey, cancellationToken);
      if (existing != null)
      {
        _logger.LogInformation("PayrollCycle for {MonthKey} already exists (Id={Id}). Returning existing.", monthKey, existing.Id);
        return existing;
      }

      // 2. Đọc cấu hình chu kỳ từ system_settings
      var settingKeys = new[] { "PAYROLL_START_DAY", "PAYROLL_END_DAY", "WEEKLY_DAYS_OFF" };
      var settings = await _settingService.GetMultipleAsync(settingKeys);

      int ParseInt(string key, int fallback) =>
          settings.TryGetValue(key, out var v) && int.TryParse(v, out var r) ? r : fallback;

      int startDay = ParseInt("PAYROLL_START_DAY", 26); // Default: chu kỳ lệch ngày 26
      int endDay = ParseInt("PAYROLL_END_DAY", 25); // Default: chốt ngày 25
      var weeklySnapshot = settings.TryGetValue("WEEKLY_DAYS_OFF", out var wo) ? wo : "6,0";
      var weeklyDaysOff = ParseWeeklyDaysOff(weeklySnapshot);

      // 3. Tính StartDate và EndDate của chu kỳ
      var (startDate, endDate) = CalculateCyclePeriod(month, year, startDay, endDay);

      _logger.LogInformation(
          "Generating PayrollCycle for {MonthKey}: StartDate={Start}, EndDate={End}",
          monthKey, startDate.ToString("dd/MM/yyyy"), endDate.ToString("dd/MM/yyyy"));

      // 4. Lấy ngày lễ trong khoảng chu kỳ
      var holidays = await _holidayRepo.GetByDateRangeAsync(startDate, endDate, cancellationToken);
      var holidayDates = holidays.Select(h => h.Date).ToList();
      int holidayCount = holidayDates.Count;

      // 5. Tính mẫu số (StandardWorkingDays)
      int standardWorkingDays = _workingDayCalculator.Calculate(
          startDate, endDate, weeklyDaysOff, holidayDates);

      if (standardWorkingDays <= 0)
      {
        _logger.LogWarning("StandardWorkingDays calculated as 0 for {MonthKey}. Clamping to 1.", monthKey);
        standardWorkingDays = 1;
      }

      _logger.LogInformation(
          "PayrollCycle {MonthKey}: StandardWorkingDays={Days} (Holidays excluded={HolidayCount})",
          monthKey, standardWorkingDays, holidayCount);

      // 6. Tạo entity và persist
      var cycle = new PayrollCycle(
          month, year, startDate, endDate,
          standardWorkingDays, weeklySnapshot, holidayCount);

      await _cycleRepo.CreateAsync(cycle, cancellationToken);

      _logger.LogInformation("PayrollCycle {MonthKey} created successfully.", monthKey);
      return cycle;
    }

    /// <inheritdoc/>
    public async Task<PayrollCycle> GetCycleAsync(
        string monthKey, CancellationToken cancellationToken = default)
    {
      var cycle = await _cycleRepo.GetByMonthKeyAsync(monthKey, cancellationToken);
      if (cycle is null)
        throw new KeyNotFoundException(
            $"Payroll cycle '{monthKey}' has not been generated yet. " +
            $"Call POST /api/payroll-cycles/generate first.");
      return cycle;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PayrollCycle>> GetCyclesByYearAsync(
        int year, CancellationToken cancellationToken = default)
    {
      return await _cycleRepo.GetByYearAsync(year, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<PayrollCycle>> GenerateBulkAsync(
        int year, CancellationToken cancellationToken = default)
    {
      var cycles = new List<PayrollCycle>();
      for (int m = 1; m <= 12; m++)
        cycles.Add(await GeneratePayrollCycleAsync(m, year, cancellationToken));

      _logger.LogInformation("BulkGenerate {Year}: {Count} payroll cycles created/confirmed.",
          year, cycles.Count);
      return cycles;
    }

    // ─── Private Helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Tính ngày bắt đầu và kết thúc thực tế của chu kỳ lương.
    ///
    /// Chu kỳ lệch (startDay &gt; 1):
    ///   StartDate = ngày <paramref name="startDay"/> của tháng TRƯỚC tháng thanh toán.
    ///   EndDate   = ngày <paramref name="endDay"/>   của tháng thanh toán.
    ///
    /// Chu kỳ chuẩn (startDay = 1):
    ///   StartDate = ngày 1 của tháng thanh toán.
    ///   EndDate   = endDay = 0 → ngày cuối tháng; hoặc ngày <paramref name="endDay"/> cụ thể.
    /// </summary>
    private static (DateTime StartDate, DateTime EndDate) CalculateCyclePeriod(
        int month, int year, int startDay, int endDay)
    {
      var currentMonthFirst = new DateTime(year, month, 1);

      DateTime startDate;
      if (startDay <= 1)
      {
        startDate = currentMonthFirst;
      }
      else
      {
        // Chu kỳ lệch: lùi về tháng trước
        var prevMonthFirst = currentMonthFirst.AddMonths(-1);
        int daysInPrev = DateTime.DaysInMonth(prevMonthFirst.Year, prevMonthFirst.Month);
        startDate = new DateTime(prevMonthFirst.Year, prevMonthFirst.Month, Math.Min(startDay, daysInPrev));
      }

      DateTime endDate;
      if (endDay <= 0)
      {
        // Ngày cuối tháng thanh toán
        endDate = currentMonthFirst.AddMonths(1).AddDays(-1);
      }
      else
      {
        int daysInCurrent = DateTime.DaysInMonth(year, month);
        endDate = new DateTime(year, month, Math.Min(endDay, daysInCurrent));
      }

      return (startDate, endDate);
    }

    private static List<DayOfWeek> ParseWeeklyDaysOff(string raw)
    {
      if (string.IsNullOrWhiteSpace(raw))
        return new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };

      return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => int.TryParse(s, out _))
                .Select(s => (DayOfWeek)int.Parse(s))
                .Distinct()
                .ToList();
    }
  }
}
