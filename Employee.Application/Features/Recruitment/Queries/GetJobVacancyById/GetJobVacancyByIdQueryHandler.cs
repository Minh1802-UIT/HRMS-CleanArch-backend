using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Recruitment.Dtos;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;

namespace Employee.Application.Features.Recruitment.Queries.GetJobVacancyById
{
  public class GetJobVacancyByIdQueryHandler : IRequestHandler<GetJobVacancyByIdQuery, JobVacancyResponseDto>
  {
    private readonly IJobVacancyRepository _repo;

    public GetJobVacancyByIdQueryHandler(IJobVacancyRepository repo)
    {
      _repo = repo;
    }

    public async Task<JobVacancyResponseDto> Handle(GetJobVacancyByIdQuery request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Job vacancy with ID {request.Id} not found.");
      return entity.ToDto();
    }
  }
}
