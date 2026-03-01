
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
    // Regex: Ch? cho phép ch?, s?, g?ch ngang, g?ch du?i
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string? ManagerId { get; set; }
    public string? ParentId { get; set; }
  }

  // 3. UPDATE DTO (Input)
  public class UpdateDepartmentDto
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // Code thu?ng không cho s?a, n?u s?a thì dùng validate tuong t? Create
    // public string Code { get; set; } = string.Empty; 
    public string? Description { get; set; }

    public string? ManagerId { get; set; }
    public string? ParentId { get; set; }
  }
}