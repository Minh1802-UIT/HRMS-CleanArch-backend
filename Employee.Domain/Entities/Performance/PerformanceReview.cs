using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.Performance
{
  public class PerformanceReview : BaseEntity
  {
    public string EmployeeId { get; private set; } = null!;
    public string ReviewerId { get; private set; } = null!;
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public PerformanceReviewStatus Status { get; private set; } = PerformanceReviewStatus.Draft;
    public double OverallScore { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    private PerformanceReview() { }

    public PerformanceReview(string employeeId, string reviewerId, DateTime periodStart, DateTime periodEnd)
    {
      if (string.IsNullOrWhiteSpace(employeeId)) throw new ArgumentException("EmployeeId is required.");
      if (string.IsNullOrWhiteSpace(reviewerId)) throw new ArgumentException("ReviewerId is required.");
      if (periodEnd < periodStart) throw new ArgumentException("PeriodEnd must be after PeriodStart.");

      EmployeeId = employeeId;
      ReviewerId = reviewerId;
      PeriodStart = periodStart;
      PeriodEnd = periodEnd;
      CreatedAt = DateTime.UtcNow;
    }

    public void UpdateReview(double score, string notes, PerformanceReviewStatus status)
    {
      if (score < 0 || score > 100) throw new ArgumentException("Score must be between 0 and 100.");
      
      OverallScore = score;
      Notes = notes;
      Status = status;
    }

    public void CompleteReview()
    {
      Status = PerformanceReviewStatus.Completed;
    }

    public void AcknowledgeReview()
    {
      Status = PerformanceReviewStatus.Acknowledged;
    }
  }
}
