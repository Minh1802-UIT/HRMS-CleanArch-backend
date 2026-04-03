using System.Collections.Generic;

namespace Employee.Domain.Common.Models
{
  public class PerformanceReviewStats
  {
    public double AverageScore { get; set; }
    public int TotalReviews { get; set; }
    public List<int> ScoreDistribution { get; set; } = new() { 0, 0, 0, 0, 0 };
  }

  public class EmployeeScoreAggregate
  {
    public string EmployeeId { get; set; } = string.Empty;
    public double AverageScore { get; set; }
    public int ReviewCount { get; set; }
  }

  public class MonthlyGoalStatsAggregate
  {
    public int Year { get; set; }
    public int Month { get; set; }
    public int Created { get; set; }
    public int Completed { get; set; }
    public int Overdue { get; set; }
  }
}
