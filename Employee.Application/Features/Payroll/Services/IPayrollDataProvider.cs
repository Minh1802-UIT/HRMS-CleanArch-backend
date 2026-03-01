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
    public decimal SocialInsuranceRate { get; set; }
    public decimal HealthInsuranceRate { get; set; }
    public decimal UnemploymentInsuranceRate { get; set; }
    public decimal InsuranceSalaryCap { get; set; }
    public decimal PersonalDeduction { get; set; }
    public decimal DependentDeduction { get; set; }
    public double StandardWorkingDays { get; set; }
    public decimal OvertimeRateNormal { get; set; }
  }
}
