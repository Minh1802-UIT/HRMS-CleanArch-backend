using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.HumanResource
{
  public class Candidate : BaseEntity
  {
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string JobVacancyId { get; set; } = null!;
    public CandidateStatus Status { get; set; } = CandidateStatus.Applied;
    public string ResumeUrl { get; set; } = string.Empty;
    public DateTime AppliedDate { get; set; }
    public int? AiScore { get; set; }
    public string? AiMatchingSummary { get; set; }
    public string? ExtractedSkills { get; set; }

    private Candidate() { }

    public Candidate(string fullName, string email, string phone, string jobVacancyId, DateTime appliedDate)
    {
      if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.");
      if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.");

      FullName = fullName;
      Email = email;
      Phone = phone;
      JobVacancyId = jobVacancyId;
      Status = CandidateStatus.Applied;
      AppliedDate = appliedDate;
      CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(CandidateStatus status)
    {
      if (Status == status)
        return; // idempotent

      // Terminal states: Rejected and Onboarded cannot be changed.
      if (Status == CandidateStatus.Rejected)
        throw new InvalidOperationException("Cannot change status of a rejected candidate.");
      if (Status == CandidateStatus.Onboarded)
        throw new InvalidOperationException("Cannot change status of an onboarded candidate.");

      // Pipeline guard: only move forward or to Rejected.
      var allowed = (Status, status) switch
      {
        (CandidateStatus.Applied,      CandidateStatus.Interviewing) => true,
        (CandidateStatus.Applied,      CandidateStatus.Rejected)     => true,
        (CandidateStatus.Interviewing, CandidateStatus.Test)         => true,
        (CandidateStatus.Interviewing, CandidateStatus.Hired)        => true,
        (CandidateStatus.Interviewing, CandidateStatus.Rejected)     => true,
        (CandidateStatus.Test,         CandidateStatus.Hired)        => true,
        (CandidateStatus.Test,         CandidateStatus.Rejected)     => true,
        (CandidateStatus.Hired,        CandidateStatus.Onboarded)    => true,
        (CandidateStatus.Hired,        CandidateStatus.Rejected)     => true,
        _ => false
      };
      if (!allowed)
        throw new InvalidOperationException($"Cannot transition candidate from '{Status}' to '{status}'.");

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

    public void UpdateAiScore(int score, string summary, string skills)
    {
      AiScore = score;
      AiMatchingSummary = summary;
      ExtractedSkills = skills;
    }
  }
}
