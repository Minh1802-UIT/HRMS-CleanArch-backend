namespace Employee.Application.Common.Dtos;

/// <summary>
/// Lightweight projection for the Employee list page.
/// Only transfers the ~500 bytes needed per employee instead of the full
/// ~5 KB EmployeeEntity (which includes PersonalInfo, BankDetails, etc.).
/// Populated by IEmployeeQueryRepository.GetPagedListAsync() via MongoDB projection.
/// Moved from Employee.Domain.Common.Models — projections belong in Application.
/// </summary>
public class EmployeeListSummary
{
    public string Id { get; set; } = string.Empty;
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
