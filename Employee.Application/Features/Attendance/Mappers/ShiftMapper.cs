using Employee.Application.Features.Attendance.Dtos;
using Employee.Domain.Entities.Attendance;
using System;

namespace Employee.Application.Features.Attendance.Mappers
{
    public static class ShiftMapper
    {
        public static ShiftDto ToDto(this Shift entity)
        {
            return new ShiftDto
            {
                Id = entity.Id,
                Name = entity.Name,
              Code = entity.Code,
                StartTime = entity.StartTime.ToString(@"hh\:mm"),
                EndTime = entity.EndTime.ToString(@"hh\:mm"),
                BreakStartTime = entity.BreakStartTime.ToString(@"hh\:mm"),
              BreakEndTime = entity.BreakEndTime.ToString(@"hh\:mm"),
                GracePeriodMinutes = entity.GracePeriodMinutes,
                IsOvernight = entity.IsOvernight,
                StandardWorkingHours = entity.StandardWorkingHours,
                IsActive = entity.IsActive
            };
        }

        public static Shift ToEntity(this CreateShiftDto dto)
    {
            double hours = (dto.EndTime - dto.StartTime).TotalHours
                         - (dto.BreakEndTime - dto.BreakStartTime).TotalHours;

            if (dto.IsOvernight) hours += 24;

      return new Shift(
          dto.Name,
          dto.Code,
          dto.StartTime,
          dto.EndTime,
          dto.BreakStartTime,
          dto.BreakEndTime,
          Math.Max(0, hours),
          dto.GracePeriodMinutes,
          dto.IsOvernight
      );
        }

        public static void UpdateFromDto(this Shift entity, UpdateShiftDto dto)
    {
            double hours = (dto.EndTime - dto.StartTime).TotalHours
                         - (dto.BreakEndTime - dto.BreakStartTime).TotalHours;
            if (dto.IsOvernight) hours += 24;

      entity.UpdateDetails(
          dto.Name,
          dto.StartTime,
          dto.EndTime,
          dto.BreakStartTime,
          dto.BreakEndTime,
          Math.Max(0, hours),
          dto.GracePeriodMinutes,
          dto.IsOvernight
      );

      if (dto.IsActive) entity.Activate(); else entity.Deactivate();
        }
    }
}