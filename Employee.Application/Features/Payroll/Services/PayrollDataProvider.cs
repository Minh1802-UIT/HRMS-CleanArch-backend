using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Attendance;
using Employee.Domain.Entities.Payroll;
using System.Globalization;

namespace Employee.Application.Features.Payroll.Services
{
  public class PayrollDataProvider : IPayrollDataProvider
  {
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IContractRepository _contractRepo;
    private readonly IContractQueryRepository _contractQueryRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IPayrollRepository _payrollRepo;
    private readonly IDepartmentRepository _deptRepo;
    private readonly IPositionRepository _positionRepo;
    private readonly ISystemSettingService _settingService;
    private readonly IPayrollCycleService _cycleService;

    public PayrollDataProvider(
        IEmployeeRepository employeeRepo,
        IContractRepository contractRepo,
        IContractQueryRepository contractQueryRepo,
        IAttendanceRepository attendanceRepo,
        IPayrollRepository payrollRepo,
        IDepartmentRepository deptRepo,
        IPositionRepository positionRepo,
        ISystemSettingService settingService,
        IPayrollCycleService cycleService)
    {
      _employeeRepo = employeeRepo;
      _contractRepo = contractRepo;
      _contractQueryRepo = contractQueryRepo;
      _attendanceRepo = attendanceRepo;
      _payrollRepo = payrollRepo;
      _deptRepo = deptRepo;
      _positionRepo = positionRepo;
      _settingService = settingService;
      _cycleService = cycleService;
    }

    public async Task<PayrollDataContainer> FetchCalculationDataAsync(string month, string year)
    {
      var monthKey = $"{month}-{year}";
      var currentMonthDate = new DateTime(int.Parse(year), int.Parse(month), 1);
      var prevMonthDate = currentMonthDate.AddMonths(-1);
      var prevMonthKey = $"{prevMonthDate.Month:D2}-{prevMonthDate.Year}";

      var container = new PayrollDataContainer
      {
        MonthKey = monthKey,
        PrevMonthKey = prevMonthKey
      };

      // 1. Đảm bảo chu kỳ lương tồn tại (idempotent: tạo mới hoặc lấy về nếu đã có)
      //    Đây là nơi StartDate, EndDate và StandardWorkingDays được tính/lấy ra bất biến.
      container.Cycle = await _cycleService.GeneratePayrollCycleAsync(
          int.Parse(month), int.Parse(year));

      // 2. Tải cấu hình bảo hiểm & thuế từ system_settings
      var settingKeys = new[] {
        "BHXH_RATE", "BHYT_RATE", "BHTN_RATE", "INSURANCE_SALARY_CAP",
        "PERSONAL_DEDUCTION", "DEPENDENT_DEDUCTION", "OT_RATE_NORMAL"
      };
      var settings = await _settingService.GetMultipleAsync(settingKeys);

      decimal Parse(string key, decimal fallback) =>
          settings.TryGetValue(key, out var val) &&
          decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : fallback;

      container.Settings = new PayrollSettings
      {
        SocialInsuranceRate = Parse("BHXH_RATE", 0.08m),
        HealthInsuranceRate = Parse("BHYT_RATE", 0.015m),
        UnemploymentInsuranceRate = Parse("BHTN_RATE", 0.01m),
        InsuranceSalaryCap = Parse("INSURANCE_SALARY_CAP", 36_000_000m),
        PersonalDeduction = Parse("PERSONAL_DEDUCTION", 11_000_000m),
        DependentDeduction = Parse("DEPENDENT_DEDUCTION", 4_400_000m),
        OvertimeRateNormal = Parse("OT_RATE_NORMAL", 1.5m)
      };

      // 3. Nhân viên & hợp đồng
      container.Employees = await _employeeRepo.GetAllActiveAsync();
      var salaryInfos = await _contractQueryRepo.GetActiveSalaryInfoAsync();
      container.SalaryMap = salaryInfos.ToDictionary(s => s.EmployeeId);

      // 4. Tên phòng ban & chức vụ
      var deptIds = container.Employees
          .Select(e => e.JobDetails?.DepartmentId)
          .Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
      var positionIds = container.Employees
          .Select(e => e.JobDetails?.PositionId)
          .Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
      container.DeptNames = await _deptRepo.GetNamesByIdsAsync(deptIds!);
      container.PositionNames = await _positionRepo.GetNamesByIdsAsync(positionIds!);

      // 5. Chấm công theo cửa sổ chu kỳ lương (hỗ trợ chu kỳ lệch)
      //    VD: Chu kỳ 02/2026 = 26/01/2026–25/02/2026  → cần lấy cả tháng 01-2026 lẫn 02-2026
      //    rồi lọc chỉ giữ DailyLogs nằm trong [StartDate, EndDate] và recalculate totals.
      var cycleMonthKeys = GetCycleMonthKeys(container.Cycle.StartDate, container.Cycle.EndDate);
      var allAttendanceBuckets = await _attendanceRepo.GetByMonthsAsync(cycleMonthKeys);
      container.AttendanceMap = BuildCycleAttendanceMap(
          allAttendanceBuckets, container.Cycle.StartDate, container.Cycle.EndDate, monthKey);

      // 6. Bảng lương hiện tại & tháng trước (dùng cho khoản nợ carry-forward)
      var allPayrolls = await _payrollRepo.GetByMonthsAsync(new[] { monthKey, prevMonthKey });
      container.PrevPayrollMap = allPayrolls.Where(p => p.Month == prevMonthKey)
          .GroupBy(p => p.EmployeeId).ToDictionary(g => g.Key, g => g.First());
      container.CurrentPayrollMap = allPayrolls.Where(p => p.Month == monthKey)
          .GroupBy(p => p.EmployeeId).ToDictionary(g => g.Key, g => g.First());

      return container;
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Trả về danh sách monthKey ("MM-YYYY") mà cửa sổ chu kỳ [start, end] trải qua.
    /// VD: start=26/01/2026, end=25/02/2026 → ["01-2026", "02-2026"]
    /// </summary>
    private static List<string> GetCycleMonthKeys(DateTime start, DateTime end)
    {
      var keys = new List<string>();
      var cur = new DateTime(start.Year, start.Month, 1);
      var last = new DateTime(end.Year, end.Month, 1);
      while (cur <= last)
      {
        keys.Add($"{cur.Month:D2}-{cur.Year}");
        cur = cur.AddMonths(1);
      }
      return keys;
    }

    /// <summary>
    /// Ghép và lọc DailyLogs từ nhiều tháng theo cửa sổ chu kỳ [startDate, endDate].
    /// Trả về map EmployeeId → AttendanceBucket với totals đã recalculate chính xác.
    /// </summary>
    private static Dictionary<string, AttendanceBucket> BuildCycleAttendanceMap(
        IEnumerable<AttendanceBucket> buckets,
        DateTime startDate,
        DateTime endDate,
        string payrollMonthKey)
    {
      var result = new Dictionary<string, AttendanceBucket>();

      foreach (var grp in buckets.GroupBy(b => b.EmployeeId))
      {
        // Gộp tất cả DailyLogs của nhân viên từ mọi bucket liên quan
        // sau đó lọc chỉ giữ lại những ngày nằm trong [startDate, endDate]
        var filteredLogs = grp
            .SelectMany(b => b.DailyLogs)
            .Where(l => l.Date.Date >= startDate.Date && l.Date.Date <= endDate.Date)
            .OrderBy(l => l.Date)
            .ToList();

        // Tạo bucket tổng hợp với monthKey của kỳ lương (VD: "02-2026")
        var merged = new AttendanceBucket(grp.Key, payrollMonthKey);
        merged.DailyLogs = filteredLogs;   // gán thẳng để tránh O(n²) qua AddOrUpdateDailyLog
        merged.RecalculateTotals();        // tính lại TotalPresent / TotalLate / TotalOvertime

        result[grp.Key] = merged;
      }

      return result;
    }
  }
}
