using Employee.Domain.Enums;
using Employee.Domain.Entities.Performance;
using Employee.Application.Features.Performance.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Employee.Application.Features.Performance.Mappers
{
  public static class PIPMapper
  {
    public static PIPResponseDto ToDto(this PIP entity, string employeeName = "", string managerName = "")
    {
      return new PIPResponseDto
      {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        EmployeeName = employeeName,
        ManagerId = entity.ManagerId,
        ManagerName = managerName,
        Title = entity.Title,
        Description = entity.Description,
        Status = entity.Status,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        OverallProgress = entity.OverallProgress,
        Objectives = entity.Objectives.Select(o => new PIPObjectiveDto
        {
          Description = o.Description,
          SuccessCriteria = o.SuccessCriteria,
          Progress = o.Progress,
          TargetDate = o.TargetDate
        }).ToList(),
        Meetings = entity.Meetings.Select(m => new PIPMeetingDto
        {
          Id = m.Id,
          MeetingDate = m.MeetingDate,
          Notes = m.Notes,
          ConductedBy = m.ConductedBy,
          CreatedAt = m.CreatedAt
        }).ToList(),
        OutcomeNotes = entity.OutcomeNotes,
        CreatedAt = entity.CreatedAt
      };
    }
  }
}
