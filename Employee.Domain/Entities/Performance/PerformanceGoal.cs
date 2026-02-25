using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.Performance
{
  public class PerformanceGoal : BaseEntity
  {
    public string EmployeeId { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime TargetDate { get; private set; }
    public double Progress { get; private set; } // 0-100
    public PerformanceGoalStatus Status { get; private set; } = PerformanceGoalStatus.InProgress;

    private PerformanceGoal() { }

    public PerformanceGoal(string employeeId, string title, string description, DateTime targetDate)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");

      EmployeeId = employeeId;
      Title = title;
      Description = description;
      TargetDate = targetDate;
      Progress = 0;
      Status = PerformanceGoalStatus.InProgress;
    }

    public void UpdateProgress(double progress)
    {
      if (progress < 0 || progress > 100) throw new ArgumentException("Progress must be between 0 and 100.");
      
      Progress = progress;
      if (Progress == 100)
      {
        Status = PerformanceGoalStatus.Completed;
      }
    }

    public void UpdateGoal(string title, string description, DateTime targetDate, PerformanceGoalStatus status)
    {
       if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.");
       
       Title = title;
       Description = description;
       TargetDate = targetDate;
       Status = status;
    }
  }
}
