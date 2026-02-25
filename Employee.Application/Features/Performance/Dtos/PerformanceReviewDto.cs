using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Performance.Dtos
{
  public class PerformanceReviewDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string ReviewerId { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public double OverallScore { get; set; }
    public string Notes { get; set; } = string.Empty;
    public PerformanceReviewStatus Status { get; set; }
  }

  public class PerformanceReviewResponseDto : PerformanceReviewDto
  {
    public string Id { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
  }
}
