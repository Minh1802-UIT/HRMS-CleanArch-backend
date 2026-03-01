namespace Employee.Domain.Common.Models;

/// <summary>
/// Lightweight projection for the Employee list page.
/// Only transfers the ~500 bytes needed per employee instead of the full
/// ~5 KB EmployeeEntity (which includes PersonalInfo, BankDetails, etc.).
/// Populated by IEmployeeRepository.GetPagedListAsync() via MongoDB projection.
/// </summary>
public class EmployeeListSummary
{
  public string Id { get; set; } = string.Empty;
  public string EmployeeCode { get; set; } = string.Empty;
  public string FullName { get; set; } = string.Empty;
  public string? AvatarUrl { get; set; }

  // Flattened from embedded JobDetails — department/position names are resolved
  // in the query handler via separate lookup calls (same as before, but now
  // those calls only transfer DepartmentId/PositionId/Status instead of the
  // whole document).
  public string DepartmentId { get; set; } = string.Empty;
  public string PositionId { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
}
