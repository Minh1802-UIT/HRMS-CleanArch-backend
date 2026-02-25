using Employee.Application.Features.Recruitment.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Recruitment.Mappers
{
  public static class JobVacancyMapper
  {
    public static JobVacancyResponseDto ToDto(this JobVacancy entity) => new()
    {
      Id = entity.Id,
      Title = entity.Title,
      Description = entity.Description,
      Vacancies = entity.Vacancies,
      ExpiredDate = entity.ExpiredDate,
      Status = entity.Status.ToString(), // Enum to String
      Requirements = entity.Requirements.ToList(),
      CreatedAt = entity.CreatedAt
    };

    public static JobVacancy ToEntity(this JobVacancyDto dto)
    {
      // Use Factory Constructor
      var entity = new JobVacancy(dto.Title, dto.Vacancies, dto.ExpiredDate);

      // Update remaining fields via domain methods
      entity.UpdateInfo(dto.Title, dto.Vacancies, dto.ExpiredDate, dto.Description);
      entity.SetRequirements(dto.Requirements);

      return entity;
    }
  }
}
