using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Entities.Leave;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Leave.Mappers
{
    public static class LeaveRequestMapper
  {
        public static LeaveRequestDto ToDto(this LeaveRequest entity, string employeeName = "Unknown", string employeeCode = "Unknown", string avatarUrl = "", string approverName = "", string? leaveTypeName = null)
        {
      double totalDays = (entity.ToDate - entity.FromDate).TotalDays + 1;

            return new LeaveRequestDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EmployeeName = employeeName,
                EmployeeCode = employeeCode,
                AvatarUrl = string.IsNullOrEmpty(avatarUrl) ? "assets/images/defaults/avatar-1.png" : avatarUrl,

              LeaveType = leaveTypeName ?? entity.LeaveType.ToString(),
                FromDate = entity.FromDate,
                ToDate = entity.ToDate,
                TotalDays = totalDays > 0 ? totalDays : 0,

                Reason = entity.Reason,
              Status = entity.Status.ToString(),
                ManagerComment = entity.ManagerComment,
              ApprovedBy = string.IsNullOrEmpty(approverName) ? entity.ApprovedBy : approverName,
                CreatedAt = entity.CreatedAt
            };
        }

        public static LeaveRequest ToEntity(this CreateLeaveRequestDto dto, string employeeId)
        {
      // Map from string to enum if possible, or use a default
      if (!Enum.TryParse<LeaveCategory>(dto.LeaveType, true, out var leaveType))
            {
        leaveType = LeaveCategory.Annual;
      }

      return new LeaveRequest(
          employeeId,
          leaveType,
          dto.FromDate,
          dto.ToDate,
          dto.Reason
      );
        }
    }
}