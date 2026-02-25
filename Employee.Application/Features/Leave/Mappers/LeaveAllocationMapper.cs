using Employee.Application.Features.Leave.Dtos;
using Employee.Domain.Entities.Leave;
using System;

namespace Employee.Application.Features.Leave.Mappers
{
  public static class LeaveAllocationMapper
  {
    public static LeaveAllocationDto ToDto(this LeaveAllocation entity, string leaveTypeName, string employeeName = "", string employeeCode = "")
    {
      return new LeaveAllocationDto
      {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        LeaveTypeId = entity.LeaveTypeId,
        EmployeeName = string.IsNullOrEmpty(employeeName) ? entity.EmployeeId : employeeName,
        EmployeeCode = employeeCode,
        LeaveTypeName = leaveTypeName,
        Year = entity.Year,
        TotalDays = entity.NumberOfDays,
        AccruedDays = entity.AccruedDays,
        UsedDays = entity.UsedDays,
        RemainingDays = entity.CurrentBalance
      };
    }

    public static LeaveAllocation ToEntity(this CreateAllocationDto dto)
    {
      // Use Factory Constructor
      return new LeaveAllocation(dto.EmployeeId, dto.LeaveTypeId, dto.Year.ToString(), dto.NumberOfDays);
    }

    public static void UpdateFromDto(this LeaveAllocation entity, UpdateAllocationDto dto)
    {
      // Need to handle updates via domain methods if necessary
      // NumberOfDays is private set
    }
  }
}