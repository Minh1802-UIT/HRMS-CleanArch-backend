
namespace Employee.Application.Features.Payroll.Dtos
{
  // ==========================================
  // 1. VIEW DTO (Output - Xem b?ng luong)
  // ==========================================
  public class PayrollDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;

    // Format: "MM-yyyy" (VD: "02-2026")
    public string Month { get; set; } = string.Empty;

    // --- Thu nh?p ---
    public decimal BaseSalary { get; set; } // Luong c?ng
    public decimal Allowances { get; set; } // T?ng ph? c?p
    public decimal Bonus { get; set; }      // Thu?ng thêm
    public decimal OvertimePay { get; set; } // Luong tang ca

    // --- Công ---
    public double TotalWorkingDays { get; set; }  // Công chu?n (VD: 22)
    public double ActualWorkingDays { get; set; } // Công th?c t? (VD: 20.5)
    public double PayableDays { get; set; }       // Công tính luong (Th?c t? + Phép nam)

    // --- T?ng k?t ---
    public decimal GrossIncome { get; set; }     // T?ng thu nh?p tru?c thu?
    public decimal TotalDeductions { get; set; } // T?ng kh?u tr? (BHXH, Thu?, Ph?t...)
    public decimal FinalNetSalary { get; set; }  // Th?c linh (Gross - Deductions)

    public string Status { get; set; } = string.Empty; // Draft, Approved, Paid, Rejected
    public DateTime? PaidDate { get; set; }

    // --- Metadata (UI hi?n th?) ---
    public string EmployeeName { get; set; } = "Unknown";
    public string EmployeeCode { get; set; } = "Unknown";
    public string DepartmentName { get; set; } = "Unknown";
    public string PositionTitle { get; set; } = "Unknown";
    public string AvatarUrl { get; set; } = "";
  }

  // ==========================================
  // 2. GENERATE DTO (Input - Yêu c?u tính luong)
  // ==========================================
  public class GeneratePayrollDto
  {
    // Tháng c?n tính luong (VD: "02-2026")
    public string Month { get; set; } = string.Empty;

    // N?u null -> Tính cho t?t c? nhân viên (Batch Job)
    // N?u có value -> Tính l?i cho 1 nhân viên c? th?
    public string? EmployeeId { get; set; }
  }

  // ==========================================
  // 3. UPDATE STATUS DTO (Input - Duy?t/Thanh toán)
  // ==========================================
  public class UpdatePayrollStatusDto
  {
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = "Approved";
  }
}