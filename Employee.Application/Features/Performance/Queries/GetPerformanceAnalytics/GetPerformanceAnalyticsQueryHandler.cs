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
      var now = DateTime.UtcNow;
      var statsFrom = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
      var statsTo = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

      var reviewStats = await _reviewRepo.GetCompletedStatsAsync(cancellationToken);
      var atRiskAggregates = await _reviewRepo.GetAtRiskEmployeesAsync(5, cancellationToken);
      var monthlyAggregates = await _goalRepo.GetMonthlyStatsAsync(statsFrom, statsTo, cancellationToken);

      var totalGoals = await _goalRepo.CountAllAsync(cancellationToken);
      var activeGoalCount = await _goalRepo.CountByStatusAsync(PerformanceGoalStatus.InProgress, cancellationToken);
      var completedGoalCount = await _goalRepo.CountByStatusAsync(PerformanceGoalStatus.Completed, cancellationToken);
      var overdueGoalCount = await _goalRepo.CountByStatusAsync(PerformanceGoalStatus.Overdue, cancellationToken);
      var avgGoalProgress = await _goalRepo.GetAverageProgressAsync(PerformanceGoalStatus.InProgress, cancellationToken);

      var activePipCount = await _pipRepo.CountByStatusAsync(PIPStatus.InProgress, cancellationToken);
      var completedPipCount = await _pipRepo.CountByStatusAsync(PIPStatus.Completed, cancellationToken);
      var failedPipCount = await _pipRepo.CountByStatusAsync(PIPStatus.Failed, cancellationToken);

      // --- Reviews ---
      var avgScore = reviewStats.TotalReviews > 0 ? reviewStats.AverageScore : 0;
      var scoreDistribution = reviewStats.ScoreDistribution?.Count == 5
        ? reviewStats.ScoreDistribution
        : new List<int> { 0, 0, 0, 0, 0 };

      // --- Monthly Goal Stats (last 6 months) ---
      var monthlyMap = monthlyAggregates.ToDictionary(x => (x.Year, x.Month));
      var monthlyStats = new List<MonthlyGoalStats>();
      for (int i = 5; i >= 0; i--)
      {
        var month = now.AddMonths(-i);
        var monthStart = new DateTime(month.Year, month.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var key = (monthStart.Year, monthStart.Month);
        monthlyMap.TryGetValue(key, out var match);
        var created = match?.Created ?? 0;
        var completed = match?.Completed ?? 0;
        var overdue = match?.Overdue ?? 0;

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
      if (atRiskAggregates.Any())
      {
        var empIds = atRiskAggregates.Select(e => e.EmployeeId).ToList();
        var empNames = await _employeeRepo.GetNamesByIdsAsync(empIds, cancellationToken);

        foreach (var emp in atRiskAggregates)
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
      var goalCompletionRate = totalGoals > 0 ? Math.Round((double)completedGoalCount / totalGoals * 100, 1) : 0;

      return new PerformanceAnalyticsDto
      {
        AverageReviewScore = Math.Round(avgScore, 1),
        TotalReviews = reviewStats.TotalReviews,
        ActiveGoals = (int)activeGoalCount,
        CompletedGoals = (int)completedGoalCount,
        OverdueGoals = (int)overdueGoalCount,
        ActivePIPs = (int)activePipCount,
        CompletedPIPs = (int)completedPipCount,
        FailedPIPs = (int)failedPipCount,
        ScoreDistribution = scoreDistribution,
        MonthlyGoalStats = monthlyStats,
        AtRiskEmployees = atRisk,
        GoalCompletionRate = goalCompletionRate,
        AverageGoalProgress = Math.Round(avgGoalProgress, 1)
      };
    }
  }
}
