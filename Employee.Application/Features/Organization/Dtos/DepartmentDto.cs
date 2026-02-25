using System.ComponentModel.DataAnnotations;

namespace Employee.Application.Features.Organization.Dtos
{
  // 1. VIEW DTO (Output)
  public class DepartmentDto
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ParentId { get; set; }
    public string? ParentName { get; set; }
    public int EmployeeCount { get; set; }
  }

  public class DepartmentNodeDto
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public List<DepartmentNodeDto> Children { get; set; } = new();
  }

  // 2. CREATE DTO (Input)
  public class CreateDepartmentDto
  {
    [Required(ErrorMessage = "Department Code is required.")]
    [MaxLength(20, ErrorMessage = "Department Code must not exceed 20 characters.")]
    // Regex: Chỉ cho phép chữ, số, gạch ngang, gạch dưới
    [RegularExpression(@"^[a-zA-Z0-9-_]+$", ErrorMessage = "Department Code can only contain letters, numbers, hyphens, and underscores.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department Name is required.")]
    [MaxLength(100, ErrorMessage = "Department Name must not exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    public string? Description { get; set; }

    public string? ManagerId { get; set; }
    public string? ParentId { get; set; }
  }

  // 3. UPDATE DTO (Input)
  public class UpdateDepartmentDto
  {
    [Required(ErrorMessage = "ID is required.")]
    public string Id { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department Name is required.")]
    [MaxLength(100, ErrorMessage = "Department Name must not exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;

    // Code thường không cho sửa, nếu sửa thì dùng validate tương tự Create
    // [MaxLength(20, ErrorMessage = "Department Code must not exceed 20 characters.")]
    // public string Code { get; set; } = string.Empty; 

    [MaxLength(500, ErrorMessage = "Description must not exceed 500 characters.")]
    public string? Description { get; set; }

    public string? ManagerId { get; set; }
    public string? ParentId { get; set; }
  }
}