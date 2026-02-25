using Employee.Application.Features.Organization.Dtos;
using Employee.Domain.Entities.Organization;
using Employee.Domain.Entities.ValueObjects;
using System;

namespace Employee.Application.Features.Organization.Mappers
{
  public static class PositionMapper
  {
    public static PositionDto ToDto(this Position entity)
    {
      if (entity == null) return null!;

      return new PositionDto
      {
        Id = entity.Id,
        Title = entity.Title,
        Code = entity.Code,
        DepartmentId = entity.DepartmentId,
        ParentId = entity.ParentId,
        SalaryRange = new SalaryRangeDto
        {
          Min = entity.SalaryRange.Min,
          Max = entity.SalaryRange.Max,
          Currency = entity.SalaryRange.Currency
        }
      };
    }

    public static Position ToEntity(this CreatePositionDto dto)
    {
      // Use Factory Constructor
      var entity = new Position(dto.Title, dto.Code, dto.DepartmentId);

      if (dto.SalaryRange != null)
      {
        entity.UpdateSalaryRange(new SalaryRange
        {
          Min = dto.SalaryRange.Min,
          Max = dto.SalaryRange.Max,
          Currency = dto.SalaryRange.Currency
        });
      }

      if (!string.IsNullOrEmpty(dto.ParentId))
      {
        entity.SetParent(dto.ParentId);
      }

      return entity;
    }

    public static void UpdateFromDto(this Position entity, UpdatePositionDto dto)
    {
      if (dto.SalaryRange != null)
      {
        entity.UpdateInfo(dto.Title ?? entity.Title, new SalaryRange
        {
          Min = dto.SalaryRange.Min,
          Max = dto.SalaryRange.Max,
          Currency = dto.SalaryRange.Currency
        });
      }
      else if (!string.IsNullOrEmpty(dto.Title))
      {
        entity.UpdateInfo(dto.Title, entity.SalaryRange);
      }

      if (!string.IsNullOrEmpty(dto.DepartmentId))
      {
        entity.ChangeDepartment(dto.DepartmentId);
      }

      entity.SetParent(dto.ParentId);
    }
  }
}