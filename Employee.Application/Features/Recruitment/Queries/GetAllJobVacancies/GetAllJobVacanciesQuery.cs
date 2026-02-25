using Employee.Application.Features.Recruitment.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Recruitment.Queries.GetAllJobVacancies
{
  public record GetAllJobVacanciesQuery() : IRequest<IEnumerable<JobVacancyResponseDto>>;
}
