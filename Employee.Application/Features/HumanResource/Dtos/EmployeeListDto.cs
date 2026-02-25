namespace Employee.Application.Features.HumanResource.Dtos;

public class EmployeeListDto
{
  public string Id { get; set; } = string.Empty;
  public string EmployeeCode { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public string DepartmentName { get; set; } = string.Empty;
  public string PositionName { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
  public string? AvatarUrl { get; set; }
}
