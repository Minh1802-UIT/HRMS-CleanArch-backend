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
            ExtractedSkills = entity.ExtractedSkills
        };

        public static Candidate ToEntity(this CandidateDto dto, DateTime appliedDate)
        {
            // Use Factory Constructor with all required fields
            var entity = new Candidate(dto.FullName, dto.Email, dto.Phone ?? string.Empty, dto.JobVacancyId, appliedDate);

            // Update optional fields via domain methods
            entity.UpdateResume(dto.ResumeUrl ?? string.Empty);

            return entity;
        }
    }
}

