using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Entities.Leave;
using System;

namespace Employee.Application.Features.Leave.Mappers
{
  public static class LeaveTypeMapper
  {
    public static LeaveTypeDto ToDto(this LeaveType entity)
    {
      return new LeaveTypeDto
      {
        Id = entity.Id,
        Name = entity.Name,
        Code = entity.Code,
        DefaultDays = entity.DefaultDaysPerYear,
        Description = entity.Description
      };
    }

    public static LeaveType ToEntity(this CreateLeaveTypeDto dto)
    {
      // Use Factory Constructor
      var code = dto.Name.ToUpper().Replace(" ", "_");
      return new LeaveType(dto.Name, code, dto.DefaultDays);
    }

    public static void UpdateFromDto(this LeaveType entity, UpdateLeaveTypeDto dto)
    {
      // UpdateSettings and SetActive are the available domain methods;
      // add UpdateInfo to LeaveType if richer updates are needed.
    }
  }
}