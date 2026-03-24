using Employee.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Employee.Application.Features.Performance.Dtos
{
  // ===== PIP Objective DTO =====

  public class PIPObjectiveDto
  {
    public string Description { get; set; } = string.Empty;
    public string SuccessCriteria { get; set; } = string.Empty;
    public double Progress { get; set; }
    public DateTime? TargetDate { get; set; }
  }

  // ===== PIP DTO =====

  public class PIPDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string ManagerId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<PIPObjectiveDto> Objectives { get; set; } = new();
  }

  public class PIPResponseDto : PIPDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public double OverallProgress { get; set; }
    public PIPStatus Status { get; set; }
    public string? OutcomeNotes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<PIPMeetingDto> Meetings { get; set; } = new();
  }

  public class PIPUpdateProgressDto
  {
    public int ObjectiveIndex { get; set; }
    public double Progress { get; set; }
  }

  public class PIPAddMeetingDto
  {
    public DateTime MeetingDate { get; set; }
    public string Notes { get; set; } = string.Empty;
  }

  public class PIPCompleteDto
  {
    public string OutcomeNotes { get; set; } = string.Empty;
    public bool Successful { get; set; } // true = Completed, false = Failed
  }

  // ===== PIP Meeting DTO =====

  public class PIPMeetingDto
  {
    public string Id { get; set; } = string.Empty;
    public DateTime MeetingDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ConductedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
  }

  // ===== Analytics DTOs =====

  public class PerformanceAnalyticsDto
  {
    /// <summary>
    /// Average score across all completed reviews.
    /// </summary>
    public double AverageReviewScore { get; set; }

    /// <summary>
    /// Total number of completed reviews.
    /// </summary>
    public int TotalReviews { get; set; }

    /// <summary>
    /// Total number of active performance goals.
    /// </summary>
    public int ActiveGoals { get; set; }

    /// <summary>
    /// Total number of completed goals.
    /// </summary>
    public int CompletedGoals { get; set; }

    /// <summary>
    /// Total number of overdue goals.
    /// </summary>
    public int OverdueGoals { get; set; }

    /// <summary>
    /// Total number of active PIPs.
    /// </summary>
    public int ActivePIPs { get; set; }

    /// <summary>
    /// Number of PIPs completed successfully.
    /// </summary>
    public int CompletedPIPs { get; set; }

    /// <summary>
    /// Number of PIPs that failed.
    /// </summary>
    public int FailedPIPs { get; set; }

    /// <summary>
    /// Score distribution: bucket 0-20, 21-40, 41-60, 61-80, 81-100.
    /// </summary>
    public List<int> ScoreDistribution { get; set; } = new() { 0, 0, 0, 0, 0 };

    /// <summary>
    /// Goal status breakdown by month for the last 6 months.
    /// </summary>
    public List<MonthlyGoalStats> MonthlyGoalStats { get; set; } = new();

    /// <summary>
    /// Top N employees with lowest average scores (at risk).
    /// </summary>
    public List<EmployeeScoreDto> AtRiskEmployees { get; set; } = new();

    /// <summary>
    /// Completion rate of goals (percentage).
    /// </summary>
    public double GoalCompletionRate { get; set; }

    /// <summary>
    /// Average goal progress across all active goals.
    /// </summary>
    public double AverageGoalProgress { get; set; }
  }

  public class MonthlyGoalStats
  {
    public string Month { get; set; } = string.Empty;
    public int Created { get; set; }
    public int Completed { get; set; }
    public int Overdue { get; set; }
  }

  public class EmployeeScoreDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public int ReviewCount { get; set; }
  }
}
