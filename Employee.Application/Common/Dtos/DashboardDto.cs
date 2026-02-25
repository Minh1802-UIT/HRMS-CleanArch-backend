namespace Employee.Application.Common.Dtos
{
  public class DashboardDto
  {
    public List<SummaryCardDto> SummaryCards { get; set; } = new();
    public AttendanceTrendDto AttendanceTrend { get; set; } = new();
    public DashboardAnalyticsDto Analytics { get; set; } = new();

    // Recruitment
    public RecruitmentStatsDto RecruitmentStats { get; set; } = new();
    public List<JobVacancyDto> RecentJobs { get; set; } = new();
    public List<OngoingProcessDto> OngoingProcesses { get; set; } = new();

    // Planning & Events
    public List<DashboardEventDto> UpcomingEvents { get; set; } = new();
    public List<DashboardLeaveDto> WhoIsOnLeave { get; set; } = new();
    public List<InterviewDto> TodayInterviews { get; set; } = new();

    // New additions for real data mapping
    public List<NewHireDto> RecentHires { get; set; } = new();
    public List<PendingRequestDto> PendingRequests { get; set; } = new();
  }

  public class DashboardAnalyticsDto
  {
    public ChartDataDto StaffDistribution { get; set; } = new();
    public ChartDataDto RecruitmentFunnel { get; set; } = new();
  }

  public class ChartDataDto
  {
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
  }

  public class SummaryCardDto
  {
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ColorScheme { get; set; } = "blue";
  }

  public class AttendanceTrendDto
  {
    public List<string> Labels { get; set; } = new();
    public List<int> Data { get; set; } = new();
  }

  public class RecruitmentStatsDto
  {
    public int JobOpenings { get; set; }
    public int NewCandidates { get; set; }
    public int Interviewed { get; set; }
    public int PendingFeedback { get; set; }
  }

  public class JobVacancyDto
  {
    public string Title { get; set; } = string.Empty;
    public int TotalCandidates { get; set; }
    public int Vacancies { get; set; }
    public DateTime ExpiredDate { get; set; }
  }

  public class OngoingProcessDto
  {
    public string CandidateName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
  }

  public class DashboardEventDto
  {
    public string Title { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Type { get; set; } = "Event"; // Event, Birthday, Anniversary
  }

  public class DashboardLeaveDto
  {
    public string EmployeeName { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
  }

  public class InterviewDto
  {
    public string CandidateName { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
  }

  public class NewHireDto
  {
    public string Name { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime JoinDate { get; set; }
    public string ColorScheme { get; set; } = "blue";
  }

  public class PendingRequestDto
  {
    public string EmployeeName { get; set; } = string.Empty;
    public string Initials { get; set; } = string.Empty;
    public string DateRange { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Leave, etc.
    public string ColorScheme { get; set; } = "purple";
  }
}
