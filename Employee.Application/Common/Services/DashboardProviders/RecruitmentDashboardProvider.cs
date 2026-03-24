using Employee.Application.Common.Dtos;
using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Domain.Common.Models;
using Employee.Domain.Entities.HumanResource;
using System.Linq;

namespace Employee.Application.Common.Services.DashboardProviders
{
  public class RecruitmentDashboardProvider : IDashboardProvider
  {
    private readonly IJobVacancyRepository _jobRepo;
    private readonly IInterviewRepository _interviewRepo;
    private readonly ICandidateRepository _candidateRepo;

    public RecruitmentDashboardProvider(IJobVacancyRepository jobRepo, IInterviewRepository interviewRepo, ICandidateRepository candidateRepo)
    {
      _jobRepo = jobRepo;
      _interviewRepo = interviewRepo;
      _candidateRepo = candidateRepo;
    }

    public async Task PopulateDashboardAsync(DashboardDto dto)
    {
      var activeJobsCount = await _jobRepo.CountActiveAsync();

      dto.SummaryCards.Add(new SummaryCardDto
      {
        Title = "Active Jobs",
        Value = activeJobsCount.ToString(),
        Icon = "work",
        ColorScheme = "green"
      });

      var interviewsToday = await _interviewRepo.GetByDateAsync(DateTime.Today);
      var interviewsTodayCount = interviewsToday.Count();

      dto.SummaryCards.Add(new SummaryCardDto
      {
        Title = "Interviews Today",
        Value = interviewsTodayCount.ToString(),
        Icon = "calendar_today",
        ColorScheme = "purple"
      });

      var statusCounts = await _candidateRepo.GetStatusCountsAsync();

      var newStatuses = new[] { "New", "Applied", "CV Applied" };
      var interviewStatuses = new[] { "Screening", "Interview", "1st Interview", "2nd Interview", "Technical Test", "Task sent" };
      var offerStatuses = new[] { "Offer" };

      var newCandidatesCount = statusCounts
        .Where(kv => newStatuses.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
        .Sum(kv => kv.Value);
      var interviewedCount = statusCounts
        .Where(kv => interviewStatuses.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
        .Sum(kv => kv.Value);
      var pendingFeedbackCount = statusCounts
        .Where(kv => offerStatuses.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
        .Sum(kv => kv.Value);

      dto.RecruitmentStats = new RecruitmentStatsDto
      {
        JobOpenings = (int)activeJobsCount,
        NewCandidates = newCandidatesCount,
        Interviewed = interviewedCount,
        PendingFeedback = pendingFeedbackCount
      };

      // Recruitment Funnel — from statusCounts already fetched above
      foreach (var (status, count) in statusCounts.OrderByDescending(x => x.Value))
      {
        dto.Analytics.RecruitmentFunnel.Labels.Add(status);
        dto.Analytics.RecruitmentFunnel.Data.Add(count);
      }
    }
  }
}
