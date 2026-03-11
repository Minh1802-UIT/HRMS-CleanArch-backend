using Employee.Domain.Common.Models;
using Employee.Application.Features.HumanResource.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Entities.ValueObjects;
using Employee.Domain.Enums;
using System;
using System.Linq;

namespace Employee.Application.Features.HumanResource.Mappers
{
  public static class ContractMapper
  {
    public static ContractDto ToDto(this ContractEntity entity)
    {
      string contractTypeStr = entity.Type switch
      {
          ContractType.FixedTerm => "Fixed-Term",
          ContractType.PartTime => "Part-Time",
          _ => entity.Type.ToString()
      };

      return new ContractDto
      {
        Id = entity.Id,
        EmployeeId = entity.EmployeeId,
        ContractCode = entity.ContractCode,
        ContractType = contractTypeStr,
        StartDate = entity.StartDate,
        EndDate = entity.EndDate,
        Status = entity.Status.ToString(),
        Salary = entity.Salary != null ? new SalaryInfoDto
        {
          BasicSalary = entity.Salary.BasicSalary,
          TotalSalary = entity.Salary.BasicSalary + entity.Salary.TransportAllowance + entity.Salary.LunchAllowance + entity.Salary.OtherAllowance
        } : new SalaryInfoDto()
      };
    }

    public static ContractEntity ToEntity(this CreateContractDto dto)
    {
      // Use Factory Constructor
      var entity = new ContractEntity(dto.EmployeeId, dto.ContractCode, dto.StartDate);

      if (Enum.TryParse<ContractType>(dto.ContractType.Replace("-", ""), true, out var parsedType))
      {
          entity.UpdateType(parsedType);
      }

      // Map SalaryComponents (immutable record)
      var salary = new SalaryComponents
      {
        BasicSalary = dto.Salary.BasicSalary,
        TransportAllowance = dto.Salary.TransportAllowance,
        LunchAllowance = dto.Salary.LunchAllowance,
        OtherAllowance = dto.Salary.OtherAllowance
      };
      entity.UpdateSalary(salary);
      entity.UpdateDates(dto.StartDate, dto.EndDate);

      return entity;
    }
  }
}
