using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Employee.Domain.Entities.Performance
{
  /// <summary>
  /// Represents a Performance Improvement Plan (PIP) for an employee.
  /// Contains structured objectives, milestones, and tracking for performance remediation.
  /// </summary>
  public class PIP : BaseEntity
  {
    public string EmployeeId { get; private set; } = null!;
    public string ManagerId { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PIPStatus Status { get; private set; } = PIPStatus.Draft;
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public double OverallProgress { get; private set; }

    /// <summary>
    /// Structured objectives that the employee must achieve.
    /// </summary>
    public List<PIPObjective> Objectives { get; private set; } = new();

    /// <summary>
    /// Periodic check-in notes recorded during the PIP.
    /// </summary>
    public List<PIPMeeting> Meetings { get; private set; } = new();

    /// <summary>
    /// Final outcome summary when the PIP concludes.
    /// </summary>
    public string? OutcomeNotes { get; private set; }

    private PIP() { }

    public PIP(string employeeId, string managerId, string title, string description,
               DateTime startDate, DateTime endDate, List<PIPObjective> objectives)
    {
      if (string.IsNullOrWhiteSpace(employeeId))
        throw new ArgumentException("EmployeeId is required.", nameof(employeeId));
      if (string.IsNullOrWhiteSpace(managerId))
        throw new ArgumentException("ManagerId is required.", nameof(managerId));
      if (string.IsNullOrWhiteSpace(title))
        throw new ArgumentException("Title is required.", nameof(title));
      if (endDate <= startDate)
        throw new ArgumentException("EndDate must be after StartDate.");

      EmployeeId = employeeId;
      ManagerId = managerId;
      Title = title;
      Description = description ?? string.Empty;
      StartDate = startDate;
      EndDate = endDate;
      Objectives = objectives ?? new List<PIPObjective>();
      OverallProgress = 0;
      Status = PIPStatus.Draft;
      CreatedAt = DateTime.UtcNow;
    }

    public void Start()
    {
      if (Status != PIPStatus.Draft)
        throw new InvalidOperationException("Only a Draft PIP can be started.");
      if (DateTime.UtcNow > EndDate)
        throw new InvalidOperationException("Cannot start a PIP after its end date.");

      Status = PIPStatus.InProgress;
      UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProgress(int objectiveIndex, double progress)
    {
      if (objectiveIndex < 0 || objectiveIndex >= Objectives.Count)
        throw new ArgumentOutOfRangeException(nameof(objectiveIndex));
      if (progress < 0 || progress > 100)
        throw new ArgumentException("Progress must be between 0 and 100.");

      Objectives[objectiveIndex].SetProgress(progress);
      RecalculateOverallProgress();
      UpdatedAt = DateTime.UtcNow;
    }

    public void AddMeeting(DateTime meetingDate, string notes, string conductedBy)
    {
      if (string.IsNullOrWhiteSpace(notes))
        throw new ArgumentException("Meeting notes are required.", nameof(notes));

      Meetings.Add(new PIPMeeting(meetingDate, notes, conductedBy));
      UpdatedAt = DateTime.UtcNow;
    }

    public void Complete(string outcomeNotes)
    {
      if (Status != PIPStatus.InProgress)
        throw new InvalidOperationException("Only an InProgress PIP can be completed.");

      Status = PIPStatus.Completed;
      OutcomeNotes = outcomeNotes;
      OverallProgress = 100;
      UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string outcomeNotes)
    {
      if (Status != PIPStatus.InProgress)
        throw new InvalidOperationException("Only an InProgress PIP can be marked as failed.");

      Status = PIPStatus.Failed;
      OutcomeNotes = outcomeNotes;
      UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
      if (Status == PIPStatus.Completed || Status == PIPStatus.Failed)
        throw new InvalidOperationException("Cannot cancel a completed or failed PIP.");

      Status = PIPStatus.Cancelled;
      OutcomeNotes = reason;
      UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string title, string description, DateTime endDate)
    {
      if (string.IsNullOrWhiteSpace(title))
        throw new ArgumentException("Title is required.");
      if (endDate <= StartDate)
        throw new ArgumentException("EndDate must be after StartDate.");

      Title = title;
      Description = description ?? string.Empty;
      EndDate = endDate;
      UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Recalculates OverallProgress as the average of all objective progress values.
    /// </summary>
    private void RecalculateOverallProgress()
    {
      if (Objectives.Count == 0) { OverallProgress = 0; return; }
      OverallProgress = Objectives.Average(o => o.Progress);

      // Auto-complete if all objectives hit 100%
      if (Objectives.All(o => o.Progress >= 100))
      {
        OverallProgress = 100;
        Status = PIPStatus.Completed;
      }
    }

    /// <summary>
    /// Auto-expires an overdue InProgress PIP.
    /// Call this during query handlers.
    /// </summary>
    public void MarkExpiredIfPastDue()
    {
      if (Status == PIPStatus.InProgress && EndDate.Date < DateTime.UtcNow.Date)
      {
        Status = PIPStatus.Failed;
        OutcomeNotes = "Automatically expired — end date passed without completion.";
      }
    }
  }

  public class PIPObjective
  {
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string Description { get; private set; } = string.Empty;
    public string SuccessCriteria { get; private set; } = string.Empty;
    public double Progress { get; private set; }
    public DateTime? TargetDate { get; private set; }

    public PIPObjective() { }

    public PIPObjective(string description, string successCriteria, DateTime? targetDate)
    {
      if (string.IsNullOrWhiteSpace(description))
        throw new ArgumentException("Objective description is required.");
      Description = description;
      SuccessCriteria = successCriteria ?? string.Empty;
      TargetDate = targetDate;
      Progress = 0;
    }

    public void SetProgress(double progress)
    {
      Progress = Math.Clamp(progress, 0, 100);
    }
  }

  public class PIPMeeting
  {
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public DateTime MeetingDate { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public string ConductedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    public PIPMeeting(DateTime meetingDate, string notes, string conductedBy)
    {
      MeetingDate = meetingDate;
      Notes = notes;
      ConductedBy = conductedBy;
      CreatedAt = DateTime.UtcNow;
    }
  }
}
