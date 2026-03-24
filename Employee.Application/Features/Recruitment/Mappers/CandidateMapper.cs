using Employee.Application.Features.Recruitment.Dtos;
using Employee.Domain.Entities.HumanResource;
using Employee.Domain.Enums;
using System;

namespace Employee.Application.Features.Recruitment.Mappers
{
    public static class CandidateMapper
    {
        public static CandidateResponseDto ToDto(this Candidate entity) => new()
        {
            Id = entity.Id,
            FullName = entity.FullName,
            Email = entity.Email,
            Phone = entity.Phone,
            JobVacancyId = entity.JobVacancyId,
            Status = entity.Status.ToString(), // Enum to String
            ResumeUrl = entity.ResumeUrl,
            AppliedDate = entity.AppliedDate,
            AiScore = entity.AiScore,
            AiMatchingSummary = entity.AiMatchingSummary,
            ExtractedSkills = entity.ExtractedSkills,
            Experience = entity.Experience,
            Education = entity.Education,
            Notes = entity.Notes
        };

        public static Candidate ToEntity(this CandidateDto dto, DateTime appliedDate)
        {
            var entity = new Candidate(dto.FullName, dto.Email, dto.Phone ?? string.Empty, dto.JobVacancyId, appliedDate);

            entity.UpdateResume(dto.ResumeUrl ?? string.Empty);
            entity.Experience = dto.Experience ?? new List<string>();
            entity.Education = dto.Education ?? new List<string>();
            entity.Notes = dto.Notes ?? new List<string>();

            return entity;
        }
    }
}

