using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Performance.Dtos;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Queries.GetPerformanceAnalytics
{
  public class GetPerformanceAnalyticsQueryHandler : IRequestHandler<GetPerformanceAnalyticsQuery, PerformanceAnalyticsDto>
  {
    private readonly IPerformanceGoalRepository _goalRepo;
    private readonly IPerformanceReviewRepository _reviewRepo;
    private readonly IPIPRepository _pipRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetPerformanceAnalyticsQueryHandler(
      IPerformanceGoalRepository goalRepo,
      IPerformanceReviewRepository reviewRepo,
      IPIPRepository pipRepo,
      IEmployeeRepository employeeRepo)
    {
      _goalRepo = goalRepo;
      _reviewRepo = reviewRepo;
      _pipRepo = pipRepo;
      _employeeRepo = employeeRepo;
    }

    public async Task<PerformanceAnalyticsDto> Handle(GetPerformanceAnalyticsQuery request, CancellationToken cancellationToken)
    {
      var allGoals = (await _goalRepo.GetAllAsync(cancellationToken)).ToList();
      var allReviews = (await _reviewRepo.GetAllAsync(cancellationToken)).ToList();
      var allPIPs = (await _pipRepo.GetAllAsync(cancellationToken)).ToList();

      // --- Goals ---
      var activeGoals = allGoals.Where(g => g.Status == PerformanceGoalStatus.InProgress).ToList();
      var completedGoals = allGoals.Where(g => g.Status == PerformanceGoalStatus.Completed).ToList();
      var overdueGoals = allGoals.Where(g => g.Status == PerformanceGoalStatus.Overdue).ToList();
      var totalGoals = allGoals.Count;

      // --- Reviews ---
      var completedReviews = allReviews.Where(r => r.Status == PerformanceReviewStatus.Completed || r.Status == PerformanceReviewStatus.Acknowledged).ToList();
      var avgScore = completedReviews.Count > 0 ? completedReviews.Average(r => r.OverallScore) : 0;

      // Score distribution buckets: 0-20, 21-40, 41-60, 61-80, 81-100
      var scoreDistribution = new List<int> { 0, 0, 0, 0, 0 };
      foreach (var review in completedReviews)
      {
        var bucket = review.OverallScore switch
        {
          <= 20 => 0,
          <= 40 => 1,
          <= 60 => 2,
          <= 80 => 3,
          _ => 4
        };
        scoreDistribution[bucket]++;
      }

      // --- PIPs ---
      var activePIPs = allPIPs.Where(p => p.Status == PIPStatus.InProgress).ToList();
      var completedPIPs = allPIPs.Where(p => p.Status == PIPStatus.Completed).ToList();
      var failedPIPs = allPIPs.Where(p => p.Status == PIPStatus.Failed).ToList();

      // --- Monthly Goal Stats (last 6 months) ---
      var monthlyStats = new List<MonthlyGoalStats>();
      for (int i = 5; i >= 0; i--)
      {
        var month = DateTime.UtcNow.AddMonths(-i);
        var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var created = allGoals.Count(g => g.CreatedAt >= monthStart && g.CreatedAt < monthEnd);
        var completed = completedGoals.Count(g => g.UpdatedAt.HasValue && g.UpdatedAt >= monthStart && g.UpdatedAt < monthEnd);
        var overdue = allGoals.Count(g => g.Status == PerformanceGoalStatus.Overdue && g.TargetDate >= monthStart && g.TargetDate < monthEnd);

        monthlyStats.Add(new MonthlyGoalStats
        {
          Month = monthStart.ToString("MMM yyyy"),
          Created = created,
          Completed = completed,
          Overdue = overdue
        });
      }

      // --- At-risk employees (lowest avg scores, top 5) ---
      var atRisk = new List<EmployeeScoreDto>();
      var employeeScores = completedReviews
        .GroupBy(r => r.EmployeeId)
        .Select(g => new
        {
          EmployeeId = g.Key,
          AverageScore = g.Average(r => r.OverallScore),
          ReviewCount = g.Count()
        })
        .OrderBy(x => x.AverageScore)
        .Take(5)
        .ToList();

      if (employeeScores.Any())
      {
        var empIds = employeeScores.Select(e => e.EmployeeId).ToList();
        var empNames = await _employeeRepo.GetNamesByIdsAsync(empIds, cancellationToken);

        foreach (var emp in employeeScores)
        {
          var nameData = empNames.TryGetValue(emp.EmployeeId, out var nd) ? nd : (null!, null!);
          atRisk.Add(new EmployeeScoreDto
          {
            EmployeeId = emp.EmployeeId,
            EmployeeName = nameData.Name,
            DepartmentName = nameData.Code, // fallback to code; dept requires deptRepo
            AverageScore = Math.Round(emp.AverageScore, 1),
            ReviewCount = emp.ReviewCount
          });
        }
      }

      // --- Rates ---
      var avgGoalProgress = activeGoals.Count > 0 ? activeGoals.Average(g => g.Progress) : 0;
      var goalCompletionRate = totalGoals > 0 ? Math.Round((double)completedGoals.Count / totalGoals * 100, 1) : 0;

      return new PerformanceAnalyticsDto
      {
        AverageReviewScore = Math.Round(avgScore, 1),
        TotalReviews = completedReviews.Count,
        ActiveGoals = activeGoals.Count,
        CompletedGoals = completedGoals.Count,
        OverdueGoals = overdueGoals.Count,
        ActivePIPs = activePIPs.Count,
        CompletedPIPs = completedPIPs.Count,
        FailedPIPs = failedPIPs.Count,
        ScoreDistribution = scoreDistribution,
        MonthlyGoalStats = monthlyStats,
        AtRiskEmployees = atRisk,
        GoalCompletionRate = goalCompletionRate,
        AverageGoalProgress = Math.Round(avgGoalProgress, 1)
      };
    }
  }
}
