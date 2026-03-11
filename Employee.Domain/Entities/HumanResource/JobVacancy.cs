using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Employee.Domain.Entities.HumanResource
{
  public class JobVacancy : BaseEntity
  {
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Vacancies { get; set; }
    public DateTime ExpiredDate { get; set; }
    public JobVacancyStatus Status { get; set; } = JobVacancyStatus.Open;
    public List<string> Requirements { get; set; } = new();

    private JobVacancy() { }

    public JobVacancy(string title, int vacancies, DateTime expiredDate)
    {
      if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
      if (vacancies <= 0) throw new ArgumentException("Vacancies must be greater than zero.");

      Title = title;
      Vacancies = vacancies;
      ExpiredDate = expiredDate;
      Status = JobVacancyStatus.Open;
      CreatedAt = DateTime.UtcNow;
    }

    public void UpdateStatus(JobVacancyStatus status)
    {
      if (Status == status)
        return; // idempotent — no-op if already in target state

      // Prevent re-opening a closed vacancy without going through Draft first.
      // Allowed: Draft ↔ Open, Open → Closed, Closed → Draft.
      var allowed = (Status, status) switch
      {
        (JobVacancyStatus.Draft,   JobVacancyStatus.Open)   => true,
        (JobVacancyStatus.Draft,   JobVacancyStatus.Closed) => true,
        (JobVacancyStatus.Open,    JobVacancyStatus.Closed) => true,
        (JobVacancyStatus.Open,    JobVacancyStatus.Draft)  => true,
        (JobVacancyStatus.Closed,  JobVacancyStatus.Draft)  => true,
        _ => false
      };
      if (!allowed)
        throw new InvalidOperationException($"Cannot transition JobVacancy from '{Status}' to '{status}'.");

      Status = status;
    }

    public void UpdateInfo(string title, int vacancies, DateTime expiredDate, string description)
    {
      if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
      if (vacancies <= 0) throw new ArgumentException("Vacancies must be greater than zero.");

      Title = title;
      Vacancies = vacancies;
      ExpiredDate = expiredDate;
      Description = description;
    }

    }
  }
}
