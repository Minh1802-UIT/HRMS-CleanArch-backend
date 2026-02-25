using Employee.Application.Features.Recruitment.Dtos;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.GetJobVacancyById
{
  public record GetJobVacancyByIdQuery(string Id) : IRequest<JobVacancyResponseDto>;
}
