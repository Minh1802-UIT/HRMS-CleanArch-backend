
namespace Employee.Application.Features.Organization.Dtos
{
  // ==========================================
  // 1. NESTED DTO (Dùng d? h?ng d? li?u luong)
  // ==========================================
  public class SalaryRangeDto
  {
    public decimal Min { get; set; }
    public decimal Max { get; set; }

    public string Currency { get; set; } = "VND"; // M?c d?nh
  }

  // ==========================================
  // 2. VIEW DTO (Output)
  // ==========================================
  public class PositionDto
  {
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // Tr? v? DTO, không tr? v? Domain Value Object
    public SalaryRangeDto? SalaryRange { get; set; }

    public string? DepartmentId { get; set; }
    // D? li?u phân c?p
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
    public string Title { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    // SalaryRange là tùy ch?n (có th? null)
    public SalaryRangeDto? SalaryRange { get; set; }
    public string DepartmentId { get; set; } = string.Empty;

    public string? ParentId { get; set; }
  }

  // ==========================================
  // 4. UPDATE DTO (Input)
  // ==========================================
  public class UpdatePositionDto
  {
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public SalaryRangeDto? SalaryRange { get; set; }
    public string? DepartmentId { get; set; }
    public string? ParentId { get; set; }
  }
}