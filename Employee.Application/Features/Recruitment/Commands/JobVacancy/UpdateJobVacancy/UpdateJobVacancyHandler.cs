using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.UpdateJobVacancy
{
  public class UpdateJobVacancyHandler : IRequestHandler<UpdateJobVacancyCommand>
  {
    private readonly IJobVacancyRepository _repo;

    public UpdateJobVacancyHandler(IJobVacancyRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(UpdateJobVacancyCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Job vacancy with ID {request.Id} not found.");

      entity.UpdateInfo(request.Dto.Title, request.Dto.Vacancies, request.Dto.ExpiredDate, request.Dto.Description);
      entity.SetRequirements(request.Dto.Requirements);

      await _repo.UpdateAsync(entity, cancellationToken);
    }
  }
}
