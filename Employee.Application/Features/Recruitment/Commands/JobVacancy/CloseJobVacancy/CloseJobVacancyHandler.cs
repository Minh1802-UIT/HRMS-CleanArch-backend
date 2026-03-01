using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Enums;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CloseJobVacancy
{
  public class CloseJobVacancyHandler : IRequestHandler<CloseJobVacancyCommand>
  {
    private readonly IJobVacancyRepository _repo;

    public CloseJobVacancyHandler(IJobVacancyRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(CloseJobVacancyCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Job vacancy with ID {request.Id} not found.");

      entity.UpdateStatus(JobVacancyStatus.Closed);
      await _repo.UpdateAsync(entity.Id, entity, cancellationToken);
    }
  }
}
