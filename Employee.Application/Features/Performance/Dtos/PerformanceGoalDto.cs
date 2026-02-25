using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Performance.Dtos
{
  public class PerformanceGoalDto
  {
    public string EmployeeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TargetDate { get; set; }
    public double Progress { get; set; }
    public PerformanceGoalStatus Status { get; set; }
  }

  public class PerformanceGoalResponseDto : PerformanceGoalDto
  {
    public string Id { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
  }
}
