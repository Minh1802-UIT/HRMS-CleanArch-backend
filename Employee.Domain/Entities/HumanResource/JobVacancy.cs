using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Employee.Domain.Entities.HumanResource
{
  public class JobVacancy : BaseEntity
  {
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int Vacancies { get; private set; }
    public DateTime ExpiredDate { get; private set; }
    public JobVacancyStatus Status { get; private set; } = JobVacancyStatus.Open;
    private readonly List<string> _requirements = new();
    public IReadOnlyCollection<string> Requirements => _requirements.AsReadOnly();

    private JobVacancy() { }

    public JobVacancy(string title, int vacancies, DateTime expiredDate)
    {
      if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
      if (vacancies <= 0) throw new ArgumentException("Vacancies must be greater than zero.");

      Title = title;
      Vacancies = vacancies;
      ExpiredDate = expiredDate;
      Status = JobVacancyStatus.Open;
    }

    public void UpdateStatus(JobVacancyStatus status)
    {
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

    public void SetRequirements(List<string> requirements)
    {
      _requirements.Clear();
      if (requirements != null)
      {
        _requirements.AddRange(requirements);
      }
    }
  }
}
