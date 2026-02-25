using Employee.Application.Features.Payroll.Dtos;
using Employee.Domain.Entities.Payroll; // Giả sử namespace Entity

namespace Employee.Application.Features.Payroll.Mappers
{
  public static class PayrollMapper
  {
    public static PayrollDto ToDto(this PayrollEntity entity, string empName = "", string empCode = "", string avatar = "")
    {
      // Use snapshot if available (Phase 7 priority)
      var name = !string.IsNullOrEmpty(entity.Snapshot.EmployeeName) ? entity.Snapshot.EmployeeName : empName;
      var code = !string.IsNullOrEmpty(entity.Snapshot.EmployeeCode) ? entity.Snapshot.EmployeeCode : empCode;
      var dept = entity.Snapshot.DepartmentName;
      var pos = entity.Snapshot.PositionTitle;

      return new PayrollDto
      {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        Month = entity.Month,

        BaseSalary = entity.BaseSalary,
        Allowances = entity.Allowances,
        Bonus = entity.Bonus,
        OvertimePay = entity.OvertimePay,

        TotalWorkingDays = entity.TotalWorkingDays,
        ActualWorkingDays = entity.ActualWorkingDays,
        PayableDays = entity.PayableDays,

        GrossIncome = entity.GrossIncome,
        TotalDeductions = entity.TotalDeductions,
        FinalNetSalary = entity.FinalNetSalary,

        Status = entity.Status.ToString(),
        PaidDate = entity.PaidDate,

        // Map Metadata from Snapshot or fallback
        EmployeeName = !string.IsNullOrEmpty(name) ? name : "Unknown",
        EmployeeCode = !string.IsNullOrEmpty(code) ? code : "Unknown",
        DepartmentName = !string.IsNullOrEmpty(dept) ? dept : "Unknown",
        PositionTitle = !string.IsNullOrEmpty(pos) ? pos : "Unknown",
        AvatarUrl = string.IsNullOrEmpty(avatar) ? "assets/images/defaults/avatar-1.png" : avatar
      };
    }
  }
}