using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.Payroll.Dtos
{
  // ==========================================
  // 1. VIEW DTO (Output - Xem bảng lương)
  // ==========================================
  public class PayrollDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;

    // Format: "MM-yyyy" (VD: "02-2026")
    public string Month { get; set; } = string.Empty;

    // --- Thu nhập ---
    public decimal BaseSalary { get; set; } // Lương cứng
    public decimal Allowances { get; set; } // Tổng phụ cấp
    public decimal Bonus { get; set; }      // Thưởng thêm
    public decimal OvertimePay { get; set; } // Lương tăng ca

    // --- Công ---
    public double TotalWorkingDays { get; set; }  // Công chuẩn (VD: 22)
    public double ActualWorkingDays { get; set; } // Công thực tế (VD: 20.5)
    public double PayableDays { get; set; }       // Công tính lương (Thực tế + Phép năm)

    // --- Tổng kết ---
    public decimal GrossIncome { get; set; }     // Tổng thu nhập trước thuế
    public decimal TotalDeductions { get; set; } // Tổng khấu trừ (BHXH, Thuế, Phạt...)
    public decimal FinalNetSalary { get; set; }  // Thực lĩnh (Gross - Deductions)

    public string Status { get; set; } = string.Empty; // Draft, Approved, Paid, Rejected
    public DateTime? PaidDate { get; set; }

    // --- Metadata (UI hiển thị) ---
    public string EmployeeName { get; set; } = "Unknown";
    public string EmployeeCode { get; set; } = "Unknown";
    public string DepartmentName { get; set; } = "Unknown";
    public string PositionTitle { get; set; } = "Unknown";
    public string AvatarUrl { get; set; } = "";
  }

  // ==========================================
  // 2. GENERATE DTO (Input - Yêu cầu tính lương)
  // ==========================================
  public class GeneratePayrollDto
  {
    [Required]
    // Tháng cần tính lương (VD: "02-2026")
    [RegularExpression(@"^\d{2}-\d{4}$", ErrorMessage = "Month format must be MM-yyyy")]
    public string Month { get; set; } = string.Empty;

    // Nếu null -> Tính cho tất cả nhân viên (Batch Job)
    // Nếu có value -> Tính lại cho 1 nhân viên cụ thể
    public string? EmployeeId { get; set; }
  }

  // ==========================================
  // 3. UPDATE STATUS DTO (Input - Duyệt/Thanh toán)
  // ==========================================
  public class UpdatePayrollStatusDto
  {
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Approved|Paid|Rejected)$", ErrorMessage = "Invalid status. Must be Approved, Paid, or Rejected.")]
    public string Status { get; set; } = "Approved";
  }
}