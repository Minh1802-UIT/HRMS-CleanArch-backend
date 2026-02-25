using Employee.Application.Features.Recruitment.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Recruitment.Queries.Candidate.GetCandidatesByVacancy
{
  public record GetCandidatesByVacancyQuery(string VacancyId) : IRequest<IEnumerable<CandidateResponseDto>>;
}
