using Employee.Application.Common.Dtos;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.Payroll;

namespace Employee.Application.Features.Payroll.Services
{
  public interface IPayrollDataProvider
  {
    Task<PayrollDataContainer> FetchCalculationDataAsync(string month, string year);
  }

  public class PayrollDataContainer
  {
    public string MonthKey { get; set; } = string.Empty;
    public string PrevMonthKey { get; set; } = string.Empty;
    public PayrollSettings Settings { get; set; } = new();
    public List<Employee.Domain.Entities.HumanResource.EmployeeEntity> Employees { get; set; } = new();
    public Dictionary<string, ContractSalaryProjection> SalaryMap { get; set; } = new();
    public Dictionary<string, string> DeptNames { get; set; } = new();
    public Dictionary<string, string> PositionNames { get; set; } = new();
    public Dictionary<string, Employee.Domain.Entities.Attendance.AttendanceBucket> AttendanceMap { get; set; } = new();
    public Dictionary<string, PayrollEntity> CurrentPayrollMap { get; set; } = new();
    public Dictionary<string, PayrollEntity> PrevPayrollMap { get; set; } = new();
  }

  public class PayrollSettings
  {
    // ── Bảo hiểm & Thuế ─────────────────────────────────────────────────────
    public decimal SocialInsuranceRate { get; set; }
    public decimal HealthInsuranceRate { get; set; }
    public decimal UnemploymentInsuranceRate { get; set; }
    public decimal InsuranceSalaryCap { get; set; }
    public decimal PersonalDeduction { get; set; }
    public decimal DependentDeduction { get; set; }
    public decimal OvertimeRateNormal { get; set; }

    // ── Chu kỳ lương (Payroll Cycle) ─────────────────────────────────────────
    /// <summary>
    /// Ngày bắt đầu chốt công trong tháng (1..28).
    /// 1 = ngày 1 của tháng (chu kỳ theo tháng dương lịch).
    /// Ví dụ: PayrollStartDay=26 → chu kỳ tính từ ngày 26 tháng trước đến PayrollEndDay tháng này.
    /// </summary>
    public int PayrollStartDay { get; set; } = 1;

    /// <summary>
    /// Ngày kết thúc chốt công (1..28 hoặc 0 = ngày cuối tháng).
    /// 0 = ngày cuối tháng dương lịch.
    /// </summary>
    public int PayrollEndDay { get; set; } = 0;

    /// <summary>
    /// Các ngày nghỉ cố định trong tuần (mặc định Saturday + Sunday).
    /// Được dùng khi tính mẫu số "ngày công chuẩn".
    /// </summary>
    public List<DayOfWeek> WeeklyDaysOff { get; set; } = new() { DayOfWeek.Saturday, DayOfWeek.Sunday };

    // ── Ngày công chuẩn – được tính ĐỘNG mỗi chu kỳ ─────────────────────────
    /// <summary>
    /// Số ngày làm việc chuẩn của chu kỳ lương này.
    /// Được tính tự động = số ngày trong chu kỳ trừ cuối tuần, trừ ngày lễ.
    /// Đây là MẪU SỐ khi tính lương prorated.
    /// </summary>
    public double StandardWorkingDays { get; set; }

    /// <summary>Ngày bắt đầu thực tế của chu kỳ lương này (đã được tính toán).</summary>
    public DateTime CycleStartDate { get; set; }

    /// <summary>Ngày kết thúc thực tế của chu kỳ lương này (đã được tính toán).</summary>
    public DateTime CycleEndDate { get; set; }
  }
}
