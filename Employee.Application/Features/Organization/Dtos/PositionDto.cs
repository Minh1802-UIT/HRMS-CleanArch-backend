using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.Organization.Dtos
{
  // ==========================================
  // 1. NESTED DTO (Dùng để hứng dữ liệu lương)
  // ==========================================
  public class SalaryRangeDto
  {
    [Range(0, double.MaxValue, ErrorMessage = "Minimum salary must be positive.")]
    public decimal Min { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Maximum salary must be positive.")]
    public decimal Max { get; set; }

    public string Currency { get; set; } = "VND"; // Mặc định
  }

  // ==========================================
  // 2. VIEW DTO (Output)
  // ==========================================
  public class PositionDto
  {
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Trả về DTO, không trả về Domain Value Object
    public SalaryRangeDto? SalaryRange { get; set; }

    public string? DepartmentId { get; set; }
    // Dữ liệu phân cấp
    public string? ParentId { get; set; }
    public string? ParentTitle { get; set; }
    public int EmployeeCount { get; set; }
  }

  public class PositionNodeDto
  {
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DepartmentId { get; set; }
    public List<PositionNodeDto> Children { get; set; } = new();
  }

  // ==========================================
  // 3. CREATE DTO (Input)
  // ==========================================
  public class CreatePositionDto
  {
    [Required(ErrorMessage = "Position Title is required.")]
    [MaxLength(100, ErrorMessage = "Title must not exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position Code is required.")]
    [MaxLength(20, ErrorMessage = "Code must not exceed 20 characters.")]
    [RegularExpression(@"^[A-Z0-9-_]+$", ErrorMessage = "Code can only contain uppercase letters, numbers, hyphens.")]
    public string Code { get; set; } = string.Empty;

    // SalaryRange là tùy chọn (có thể null)
    public SalaryRangeDto? SalaryRange { get; set; }

    [Required(ErrorMessage = "DepartmentId is required.")]
    public string DepartmentId { get; set; } = string.Empty;

    public string? ParentId { get; set; }
  }

  // ==========================================
  // 4. UPDATE DTO (Input)
  // ==========================================
  public class UpdatePositionDto
  {
    [Required]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position Title is required.")]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    public SalaryRangeDto? SalaryRange { get; set; }
    public string? DepartmentId { get; set; }
    public string? ParentId { get; set; }
  }
}