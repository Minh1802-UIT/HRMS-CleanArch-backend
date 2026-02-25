using Employee.Application.Features.Organization.Dtos;
using Employee.Domain.Entities.Organization;
using System;

namespace Employee.Application.Features.Organization.Mappers
{
  public static class DepartmentMapper
  {
    public static DepartmentDto? ToDto(this Department department)
    {
      if (department == null) return null;

      return new DepartmentDto
      {
        Id = department.Id,
        Name = department.Name,
        Code = department.Code,
        Description = department.Description,
        ManagerId = department.ManagerId,
        ParentId = department.ParentId,
        EmployeeCount = 0
      };
    }

    public static Department ToEntity(this CreateDepartmentDto dto)
    {
      // Use Factory Constructor
      var entity = new Department(dto.Name ?? string.Empty, dto.Code ?? string.Empty);

      // Update remaining info via domain method
      entity.UpdateInfo(dto.Name ?? entity.Name, dto.Description ?? string.Empty);
      entity.AssignManager(dto.ManagerId);
      entity.SetParent(dto.ParentId);
      {
        // Handle ParentId if needed via domain method
      }

      return entity;
    }
  }
}