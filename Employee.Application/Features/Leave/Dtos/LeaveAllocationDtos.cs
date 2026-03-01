
namespace Employee.Application.Features.Leave.Dtos
{
  // ==========================================
  // VIEW (Output)
  // ==========================================
  public class LeaveAllocationDto
  {
    public string Id { get; set; } = string.Empty; // Added ID
    public string EmployeeId { get; set; } = string.Empty; // Added EmployeeId
    public string LeaveTypeId { get; set; } = string.Empty; // Added LeaveTypeId
    public string EmployeeName { get; set; } = string.Empty; // Added EmployeeName
    public string EmployeeCode { get; set; } = string.Empty; // Added EmployeeCode
    public string LeaveTypeName { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty; // Added Year
    public double TotalDays { get; set; } // T?ng du?c c?p (Annual Quota)
    public double AccruedDays { get; set; } // Added Accrued
    public double UsedDays { get; set; }  // Ðã dùng
    public double RemainingDays { get; set; } // Còn l?i
  }

  // ==========================================
  // CREATE (Input - C?p m?i d?u nam)
  // ==========================================
  public class CreateAllocationDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string LeaveTypeId { get; set; } = string.Empty;
    public double NumberOfDays { get; set; }

    public int Year { get; set; }
  }

  // ==========================================
  // UPDATE (Input - S?a l?i s? du n?u c?p sai)
  // ==========================================
  public class UpdateAllocationDto
  {
    public string Id { get; set; } = string.Empty;
    public double NumberOfDays { get; set; }
  }
}