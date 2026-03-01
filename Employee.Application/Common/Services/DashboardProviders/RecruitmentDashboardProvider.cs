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

      // Interviews Today (Simplified)
      dto.SummaryCards.Add(new SummaryCardDto
      {
        Title = "Interviews Today",
        Value = "0", // Logic would go here if we had date-based interview fetching
        Icon = "calendar_today",
        ColorScheme = "purple"
      });

      dto.RecruitmentStats = new RecruitmentStatsDto
      {
        JobOpenings = (int)activeJobsCount,
        NewCandidates = 0,
        Interviewed = 0,
        PendingFeedback = 0
      };

      // Recruitment Funnel — SERVER-SIDE AGGREGATION
      var statusCounts = await _candidateRepo.GetStatusCountsAsync();
      foreach (var (status, count) in statusCounts.OrderByDescending(x => x.Value))
      {
        dto.Analytics.RecruitmentFunnel.Labels.Add(status);
        dto.Analytics.RecruitmentFunnel.Data.Add(count);
      }
    }
  }
}
