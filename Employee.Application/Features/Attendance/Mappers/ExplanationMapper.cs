using Employee.Application.Features.Attendance.Dtos;
using Employee.Domain.Entities.Attendance;

namespace Employee.Application.Features.Attendance.Mappers
{
  public static class ExplanationMapper
  {
    public static AttendanceExplanationDto ToDto(this AttendanceExplanation e, string? employeeName = null) =>
        new AttendanceExplanationDto
        {
          Id = e.Id,
          EmployeeId = e.EmployeeId,
          EmployeeName = employeeName,
          WorkDate = e.WorkDate,
          Reason = e.Reason,
          Status = e.Status.ToString(),
          ReviewedBy = e.ReviewedBy,
          ReviewNote = e.ReviewNote,
          ReviewedAt = e.ReviewedAt,
          CreatedAt = e.CreatedAt
        };
  }
}
