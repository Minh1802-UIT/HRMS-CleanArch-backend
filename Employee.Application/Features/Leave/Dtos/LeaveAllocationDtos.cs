using System.ComponentModel.DataAnnotations;

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
    public double TotalDays { get; set; } // Tổng được cấp (Annual Quota)
    public double AccruedDays { get; set; } // Added Accrued
    public double UsedDays { get; set; }  // Đã dùng
    public double RemainingDays { get; set; } // Còn lại
  }

  // ==========================================
  // CREATE (Input - Cấp mới đầu năm)
  // ==========================================
  public class CreateAllocationDto
  {
    [Required(ErrorMessage = "Employee ID is required.")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Leave Type ID is required.")]
    public string LeaveTypeId { get; set; } = string.Empty;

    [Range(0, 365, ErrorMessage = "Number of days must be valid.")]
    public double NumberOfDays { get; set; }

    public int Year { get; set; }
  }

  // ==========================================
  // UPDATE (Input - Sửa lại số dư nếu cấp sai)
  // ==========================================
  public class UpdateAllocationDto
  {
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    [Range(0, 365, ErrorMessage = "Number of days must be valid.")]
    public double NumberOfDays { get; set; }
  }
}