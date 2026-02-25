using Employee.Application.Features.Performance.Dtos;
using Employee.Domain.Entities.Performance;

namespace Employee.Application.Features.Performance.Mappers
{
  public static class PerformanceMapper
  {
    public static PerformanceReviewResponseDto ToDto(this PerformanceReview entity, string employeeName = "", string reviewerName = "")
    {
      return new PerformanceReviewResponseDto
      {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        ReviewerId = entity.ReviewerId,
        PeriodStart = entity.PeriodStart,
        PeriodEnd = entity.PeriodEnd,
        OverallScore = entity.OverallScore,
        Notes = entity.Notes,
        Status = entity.Status,
        EmployeeName = employeeName,
        ReviewerName = reviewerName,
        CreatedAt = entity.CreatedAt
      };
    }

    public static PerformanceGoalResponseDto ToDto(this PerformanceGoal entity)
    {
      return new PerformanceGoalResponseDto
      {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        Title = entity.Title,
        Description = entity.Description,
        TargetDate = entity.TargetDate,
        Progress = entity.Progress,
        Status = entity.Status,
        CreatedAt = entity.CreatedAt
      };
    }
  }
}
