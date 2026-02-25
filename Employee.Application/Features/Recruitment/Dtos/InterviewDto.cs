namespace Employee.Application.Features.Recruitment.Dtos
{
  public class InterviewDto
  {
    public string CandidateId { get; set; } = string.Empty;
    public string InterviewerId { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
    public string Location { get; set; } = "Online";
  }

  public class InterviewResponseDto
  {
    public string Id { get; set; } = string.Empty;
    public string CandidateId { get; set; } = string.Empty;
    public string InterviewerId { get; set; } = string.Empty;
    public DateTime ScheduledTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
  }

  public class UpdateInterviewResultDto
  {
    public string Status { get; set; } = string.Empty; // Scheduled, Completed, Cancelled
    public string Feedback { get; set; } = string.Empty;
  }

  public class ReviewInterviewDto
  {
    public string Result { get; set; } = string.Empty; // Status
    public string Notes { get; set; } = string.Empty; // Feedback
  }
}
