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

    public PayrollDataProvider(
        IEmployeeRepository employeeRepo,
        IContractRepository contractRepo,
        IContractQueryRepository contractQueryRepo,
        IAttendanceRepository attendanceRepo,
        IPayrollRepository payrollRepo,
        IDepartmentRepository deptRepo,
        IPositionRepository positionRepo,
        ISystemSettingService settingService)
    {
      _employeeRepo = employeeRepo;
      _contractRepo = contractRepo;
      _contractQueryRepo = contractQueryRepo;
      _attendanceRepo = attendanceRepo;
      _payrollRepo = payrollRepo;
      _deptRepo = deptRepo;
      _positionRepo = positionRepo;
      _settingService = settingService;
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
      var settingKeys = new[] { "BHXH_RATE", "BHYT_RATE", "BHTN_RATE", "INSURANCE_SALARY_CAP",
                "PERSONAL_DEDUCTION", "DEPENDENT_DEDUCTION", "STANDARD_WORKING_DAYS", "OT_RATE_NORMAL" };
      var settings = await _settingService.GetMultipleAsync(settingKeys);

      decimal ParseSetting(string key, decimal fallback) =>
          settings.TryGetValue(key, out var val) && decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : fallback;

      container.Settings = new PayrollSettings
      {
        SocialInsuranceRate = ParseSetting("BHXH_RATE", 0.08m),
        HealthInsuranceRate = ParseSetting("BHYT_RATE", 0.015m),
        UnemploymentInsuranceRate = ParseSetting("BHTN_RATE", 0.01m),
        InsuranceSalaryCap = ParseSetting("INSURANCE_SALARY_CAP", 36000000m),
        PersonalDeduction = ParseSetting("PERSONAL_DEDUCTION", 11000000m),
        DependentDeduction = ParseSetting("DEPENDENT_DEDUCTION", 4400000m),
        StandardWorkingDays = (double)ParseSetting("STANDARD_WORKING_DAYS", 26m),
        OvertimeRateNormal = ParseSetting("OT_RATE_NORMAL", 1.5m)
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
  }
}
