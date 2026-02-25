using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.HumanResource.Dtos
{
  // ----------------------------------------------------
  // 1. SHARED DTOs (Dùng chung cho cả Create và Update)
  // ----------------------------------------------------

  public class SalaryInfoInputDto
  {
    [Required(ErrorMessage = "Basic Salary is required.")]
    [Range(0, double.MaxValue, ErrorMessage = "Basic Salary must be a positive number.")]
    public decimal BasicSalary { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Transport Allowance must be a positive number.")]
    public decimal TransportAllowance { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Lunch Allowance must be a positive number.")]
    public decimal LunchAllowance { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Other Allowance must be a positive number.")]
    public decimal OtherAllowance { get; set; }
  }

  // ----------------------------------------------------
  // 2. VIEW DTO (Output - Trả về cho Frontend)
  // ----------------------------------------------------
  public class ContractDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty; // FE cần tên để hiển thị
    public string ContractCode { get; set; } = string.Empty;
    public string ContractType { get; set; } = string.Empty; // Fixed-Term, Indefinite...
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = string.Empty; // Active, Expired, Terminated

    // Thông tin lương (Flatten hoặc Nested tùy convention, ở đây để Nested cho gọn)
    public SalaryInfoDto Salary { get; set; } = new();
  }

  public class SalaryInfoDto
  {
    public decimal BasicSalary { get; set; }
    public decimal TotalSalary { get; set; } // Gross salary (Basic + Allowances)
                                             // ... các phụ cấp khác
  }

  // ----------------------------------------------------
  // 3. CREATE DTO (Input)
  // ----------------------------------------------------
  public class CreateContractDto
  {
    [Required(ErrorMessage = "Employee ID is required.")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contract Code is required.")]
    [MaxLength(20, ErrorMessage = "Contract Code must not exceed 20 characters.")]
    // Mã hợp đồng: Cho phép cả chữ hoa, chữ thường, số, dấu gạch
    [RegularExpression(@"^[a-zA-Z0-9-_]+$", ErrorMessage = "Contract Code can only contain letters, numbers, hyphens, and underscores.")]
    public string ContractCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Contract Type is required.")]
    // Có thể validate cứng các loại hợp đồng ở đây nếu muốn (VD: Probation, Official...)
    public string ContractType { get; set; } = "Fixed-Term";

    [Required(ErrorMessage = "Start Date is required.")]
    public DateTime StartDate { get; set; }

    // EndDate có thể null (Hợp đồng không xác định thời hạn)
    // Lưu ý: Logic "EndDate > StartDate" nên để Service check hoặc Custom Attribute
    public DateTime? EndDate { get; set; }

    [Required(ErrorMessage = "Salary information is required.")]
    public SalaryInfoInputDto Salary { get; set; } = new();
  }

  // ----------------------------------------------------
  // 4. UPDATE DTO (Input)
  // ----------------------------------------------------
  public class UpdateContractDto
  {
    [Required]
    public string Id { get; set; } = string.Empty;

    // Mã hợp đồng (ContractCode) và EmployeeId thường KHÔNG được sửa.
    // Chỉ cho sửa ngày kết thúc (Gia hạn) hoặc thông tin lương.

    public DateTime? EndDate { get; set; }

    // Nếu cập nhật cả lương
    public SalaryInfoInputDto? Salary { get; set; }

    // Trạng thái (VD: Chấm dứt hợp đồng sớm)
    public string? Status { get; set; }
  }
}