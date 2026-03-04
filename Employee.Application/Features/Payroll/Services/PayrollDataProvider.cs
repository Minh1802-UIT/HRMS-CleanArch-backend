using Employee.Application.Common.Interfaces;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Payroll;
using Employee.Domain.Entities.HumanResource;
using System.Globalization;
// ContractSalaryProjection is now in Application.Common.Dtos — no extra using needed (same assembly)

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
    private readonly IPublicHolidayRepository _holidayRepo;
    private readonly IWorkingDayCalculator _workingDayCalculator;

    public PayrollDataProvider(
        IEmployeeRepository employeeRepo,
        IContractRepository contractRepo,
        IContractQueryRepository contractQueryRepo,
        IAttendanceRepository attendanceRepo,
        IPayrollRepository payrollRepo,
        IDepartmentRepository deptRepo,
        IPositionRepository positionRepo,
        ISystemSettingService settingService,
        IPublicHolidayRepository holidayRepo,
        IWorkingDayCalculator workingDayCalculator)
    {
      _employeeRepo = employeeRepo;
      _contractRepo = contractRepo;
      _contractQueryRepo = contractQueryRepo;
      _attendanceRepo = attendanceRepo;
      _payrollRepo = payrollRepo;
      _deptRepo = deptRepo;
      _positionRepo = positionRepo;
      _settingService = settingService;
      _holidayRepo = holidayRepo;
      _workingDayCalculator = workingDayCalculator;
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

      // 1. Settings
      var settingKeys = new[] {
        "BHXH_RATE", "BHYT_RATE", "BHTN_RATE", "INSURANCE_SALARY_CAP",
        "PERSONAL_DEDUCTION", "DEPENDENT_DEDUCTION", "OT_RATE_NORMAL",
        "PAYROLL_START_DAY", "PAYROLL_END_DAY", "WEEKLY_DAYS_OFF"
      };
      var settings = await _settingService.GetMultipleAsync(settingKeys);

      decimal ParseDecimal(string key, decimal fallback) =>
          settings.TryGetValue(key, out var val) && decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : fallback;

      int ParseInt(string key, int fallback) =>
          settings.TryGetValue(key, out var val) && int.TryParse(val, out var r) ? r : fallback;

      // Parse weekly days off: stored as "6,0" (DayOfWeek int values: Saturday=6, Sunday=0)
      List<DayOfWeek> ParseWeeklyDaysOff(string key)
      {
        if (!settings.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
          return new List<DayOfWeek> { DayOfWeek.Saturday, DayOfWeek.Sunday };

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(s => (DayOfWeek)int.Parse(s))
            .Distinct()
            .ToList();
      }

      int payrollStartDay = ParseInt("PAYROLL_START_DAY", 1);
      int payrollEndDay = ParseInt("PAYROLL_END_DAY", 0);
      var weeklyDaysOff = ParseWeeklyDaysOff("WEEKLY_DAYS_OFF");

      // 2. Tính chu kỳ lương (Payroll Cycle Period)
      var (cycleStart, cycleEnd) = CalculateCyclePeriod(
          int.Parse(month), int.Parse(year), payrollStartDay, payrollEndDay);

      // 3. Lấy ngày lễ trong chu kỳ và tính mẫu số ngày công chuẩn
      var holidays = await _holidayRepo.GetByDateRangeAsync(cycleStart, cycleEnd);
      var holidayDates = holidays.Select(h => h.Date).ToList();

      int standardWorkingDays = _workingDayCalculator.Calculate(
          cycleStart, cycleEnd, weeklyDaysOff, holidayDates);

      // Đảm bảo luôn có mẫu số hợp lệ (không chia cho 0)
      if (standardWorkingDays <= 0) standardWorkingDays = 22;

      container.Settings = new PayrollSettings
      {
        SocialInsuranceRate = ParseDecimal("BHXH_RATE", 0.08m),
        HealthInsuranceRate = ParseDecimal("BHYT_RATE", 0.015m),
        UnemploymentInsuranceRate = ParseDecimal("BHTN_RATE", 0.01m),
        InsuranceSalaryCap = ParseDecimal("INSURANCE_SALARY_CAP", 36000000m),
        PersonalDeduction = ParseDecimal("PERSONAL_DEDUCTION", 11000000m),
        DependentDeduction = ParseDecimal("DEPENDENT_DEDUCTION", 4400000m),
        OvertimeRateNormal = ParseDecimal("OT_RATE_NORMAL", 1.5m),
        // Chu kỳ lương
        PayrollStartDay = payrollStartDay,
        PayrollEndDay = payrollEndDay,
        WeeklyDaysOff = weeklyDaysOff,
        CycleStartDate = cycleStart,
        CycleEndDate = cycleEnd,
        // Mẫu số được tính động — cốt lõi của logic nghiệp vụ mới
        StandardWorkingDays = standardWorkingDays
      };

      // 2. Employees & Contracts
      container.Employees = await _employeeRepo.GetAllActiveAsync();

      var salaryInfos = await _contractQueryRepo.GetActiveSalaryInfoAsync();
      container.SalaryMap = salaryInfos.ToDictionary(s => s.EmployeeId);

      // 3. Organization Names
      var deptIds = container.Employees.Select(e => e.JobDetails?.DepartmentId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
      var positionIds = container.Employees.Select(e => e.JobDetails?.PositionId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
      container.DeptNames = await _deptRepo.GetNamesByIdsAsync(deptIds!);
      container.PositionNames = await _positionRepo.GetNamesByIdsAsync(positionIds!);

      // 4. Attendance (use first per employee to avoid duplicate key exception)
      var attendanceBuckets = await _attendanceRepo.GetByMonthAsync(monthKey);
      container.AttendanceMap = attendanceBuckets
          .GroupBy(b => b.EmployeeId)
          .ToDictionary(g => g.Key, g => g.First());

      // 5. Payrolls
      var allPayrolls = await _payrollRepo.GetByMonthsAsync(new[] { monthKey, prevMonthKey });
      container.PrevPayrollMap = allPayrolls.Where(p => p.Month == prevMonthKey)
          .GroupBy(p => p.EmployeeId)
          .ToDictionary(g => g.Key, g => g.First());
      container.CurrentPayrollMap = allPayrolls.Where(p => p.Month == monthKey)
          .GroupBy(p => p.EmployeeId)
          .ToDictionary(g => g.Key, g => g.First());

      return container;
    }

    /// <summary>
    /// Tính ngày bắt đầu và kết thúc thực tế của chu kỳ lương dựa trên cấu hình.
    /// </summary>
    /// <param name="month">Tháng thanh toán lương (1-12).</param>
    /// <param name="year">Năm thanh toán lương.</param>
    /// <param name="startDay">PAYROLL_START_DAY: ngày bắt đầu chấm công (1..28).</param>
    /// <param name="endDay">PAYROLL_END_DAY: ngày kết thúc (0 = cuối tháng, 1..28 = ngày cụ thể).</param>
    private static (DateTime StartDate, DateTime EndDate) CalculateCyclePeriod(
        int month, int year, int startDay, int endDay)
    {
      var currentMonthFirst = new DateTime(year, month, 1);
      DateTime startDate;
      DateTime endDate;

      if (startDay <= 1)
      {
        // Chu kỳ chuẩn: từ ngày 1 của tháng
        startDate = currentMonthFirst;
      }
      else
      {
        // Chu kỳ lệch (cutoff): từ ngày <startDay> của tháng TRƯỚC
        var prevMonthFirst = currentMonthFirst.AddMonths(-1);
        int daysInPrevMonth = DateTime.DaysInMonth(prevMonthFirst.Year, prevMonthFirst.Month);
        int actualStartDay = Math.Min(startDay, daysInPrevMonth);
        startDate = new DateTime(prevMonthFirst.Year, prevMonthFirst.Month, actualStartDay);
      }

      if (endDay <= 0)
      {
        // Ngày cuối tháng hiện tại
        endDate = currentMonthFirst.AddMonths(1).AddDays(-1);
      }
      else
      {
        int daysInCurrentMonth = DateTime.DaysInMonth(year, month);
        endDate = new DateTime(year, month, Math.Min(endDay, daysInCurrentMonth));
      }

      return (startDate, endDate);
    }
  }
}
