using Employee.Domain.Entities.Common;
using Employee.Domain.Enums;
using System;

namespace Employee.Domain.Entities.HumanResource
{
  public class Interview : BaseEntity
  {
    public string CandidateId { get; private set; } = null!;
    public string InterviewerId { get; private set; } = null!; // EmployeeId
    public DateTime ScheduledTime { get; private set; }
    public int DurationMinutes { get; private set; } = 60;
    public string Location { get; private set; } = "Online";
    public InterviewStatus Status { get; private set; } = InterviewStatus.Scheduled;
    public string Feedback { get; private set; } = string.Empty;

    private Interview() { }

    public Interview(string candidateId, string interviewerId, DateTime scheduledTime, int durationMinutes = 60, string location = "Online")
    {
      if (string.IsNullOrWhiteSpace(candidateId)) throw new ArgumentException("CandidateId is required.");
      if (string.IsNullOrWhiteSpace(interviewerId)) throw new ArgumentException("InterviewerId is required.");

      CandidateId = candidateId;
      InterviewerId = interviewerId;
      ScheduledTime = scheduledTime;
      DurationMinutes = durationMinutes;
      Location = location;
      Status = InterviewStatus.Scheduled;
    }

    public void Complete(string feedback)
    {
      Status = InterviewStatus.Completed;
      Feedback = feedback;
    }

    public void Cancel()
    {
      Status = InterviewStatus.Cancelled;
    }
  }
}
