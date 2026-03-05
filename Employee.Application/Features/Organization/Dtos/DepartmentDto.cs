
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
    public string? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerCode { get; set; }
    public int EmployeeCount { get; set; }
    public List<DepartmentNodeDto> Children { get; set; } = new();
  }

  // 2. CREATE DTO (Input)
  public class CreateDepartmentDto
  {
    // Only letters, digits, hyphens, and underscores are allowed
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

    // Code is normally not editable; validate the same way as Create if it is
    // public string Code { get; set; } = string.Empty; 
    public string? Description { get; set; }

    public string? ManagerId { get; set; }
    public string? ParentId { get; set; }
  }
}