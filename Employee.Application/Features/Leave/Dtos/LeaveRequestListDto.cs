namespace Employee.Application.Features.Leave.Dtos;

public class LeaveRequestListDto
{
  public string Id { get; set; } = string.Empty;
  public string EmployeeCode { get; set; } = string.Empty;
  public string EmployeeName { get; set; } = string.Empty;
  public string? AvatarUrl { get; set; }
  public string LeaveType { get; set; } = string.Empty;
  public DateTime FromDate { get; set; }
  public DateTime ToDate { get; set; }
  public string Status { get; set; } = string.Empty;
  public string? Reason { get; set; }
}
