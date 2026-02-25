using Employee.Application.Features.Recruitment.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Recruitment.Mappers
{
  public static class InterviewMapper
  {
    public static InterviewResponseDto ToDto(this Interview entity) => new()
    {
      Id = entity.Id,
      CandidateId = entity.CandidateId,
      InterviewerId = entity.InterviewerId,
      ScheduledTime = entity.ScheduledTime,
      DurationMinutes = entity.DurationMinutes,
      Location = entity.Location,
      Status = entity.Status.ToString(), // Enum to String
      Feedback = entity.Feedback
    };

    public static Interview ToEntity(this InterviewDto dto)
    {
      // Use Factory Constructor
      return new Interview(
          dto.CandidateId,
          dto.InterviewerId,
          dto.ScheduledTime,
          dto.DurationMinutes,
          dto.Location
      );
    }
  }
}
