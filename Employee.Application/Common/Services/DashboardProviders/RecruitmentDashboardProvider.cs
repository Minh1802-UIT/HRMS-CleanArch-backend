using Employee.Application.Common.Dtos;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Models;
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
        Icon = "briefcase",
        ColorScheme = "green"
      });

      // Interviews Today (Simplified)
      dto.SummaryCards.Add(new SummaryCardDto
      {
        Title = "Interviews Today",
        Value = "0", // Logic would go here if we had date-based interview fetching
        Icon = "calendar",
        ColorScheme = "purple"
      });

      dto.RecruitmentStats = new RecruitmentStatsDto
      {
        JobOpenings = (int)activeJobsCount,
        NewCandidates = 0,
        Interviewed = 0,
        PendingFeedback = 0
      };

      // Recruitment Funnel Calculation
      var allCandidates = await _candidateRepo.GetAllAsync();
      var funnel = allCandidates
        .GroupBy(c => c.Status)
        .Select(g => new
        {
          Status = g.Key,
          Count = g.Count()
        })
        .OrderByDescending(x => x.Count)
        .ToList();

      foreach (var item in funnel)
      {
        dto.Analytics.RecruitmentFunnel.Labels.Add(item.Status.ToString());
        dto.Analytics.RecruitmentFunnel.Data.Add(item.Count);
      }
    }
  }
}
