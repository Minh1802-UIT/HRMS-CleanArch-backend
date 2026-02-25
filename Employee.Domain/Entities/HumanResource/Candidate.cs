using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.HumanResource
{
  public class Candidate : BaseEntity
  {
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string JobVacancyId { get; private set; } = null!;
    public CandidateStatus Status { get; private set; } = CandidateStatus.Applied;
    public string ResumeUrl { get; private set; } = string.Empty;
    public DateTime AppliedDate { get; private set; } = DateTime.UtcNow;

    private Candidate() { }

    public Candidate(string fullName, string email, string phone, string jobVacancyId)
    {
      if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.");
      if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");

      FullName = fullName;
      Email = email;
      Phone = phone;
      JobVacancyId = jobVacancyId;
      Status = CandidateStatus.Applied;
      AppliedDate = DateTime.UtcNow;
    }

    public void UpdateStatus(CandidateStatus status)
    {
      Status = status;
    }

    public void UpdateInfo(string fullName, string email, string phone)
    {
      if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.");
      if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");

      FullName = fullName;
      Email = email;
      Phone = phone;
    }

    public void UpdateResume(string url)
    {
      ResumeUrl = url;
    }
  }
}
