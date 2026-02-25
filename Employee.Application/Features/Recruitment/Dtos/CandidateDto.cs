namespace Employee.Application.Features.Recruitment.Dtos
{
  public class CandidateDto
  {
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string JobVacancyId { get; set; } = string.Empty;
    public string ResumeUrl { get; set; } = string.Empty;
  }

  public class CandidateResponseDto
  {
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string JobVacancyId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ResumeUrl { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
  }

  public class UpdateCandidateStatusDto
  {
    public string Status { get; set; } = string.Empty; // Applied, Interviewing, Test, Hired, Rejected
  }

  public class OnboardCandidateDto
  {
    public string EmployeeCode { get; set; } = string.Empty;
    public string DepartmentId { get; set; } = string.Empty;
    public string PositionId { get; set; } = string.Empty;
    public string? ManagerId { get; set; }
    public DateTime JoinDate { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Date of birth of the new employee (required for age validation ≥ 18 in PersonalInfo).
    /// </summary>
    public DateTime DateOfBirth { get; set; }
  }
}
