using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.CreateJobVacancy
{
  public class CreateJobVacancyHandler : IRequestHandler<CreateJobVacancyCommand>
  {
    private readonly IJobVacancyRepository _repo;

    public CreateJobVacancyHandler(IJobVacancyRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(CreateJobVacancyCommand request, CancellationToken cancellationToken)
    {
      var entity = request.Dto.ToEntity();
      await _repo.CreateAsync(entity, cancellationToken);
    }
  }
}
