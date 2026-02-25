using Employee.Application.Common.Interfaces.Organization.IRepository;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.JobVacancy.DeleteJobVacancy
{
  public class DeleteJobVacancyHandler : IRequestHandler<DeleteJobVacancyCommand>
  {
    private readonly IJobVacancyRepository _repo;

    public DeleteJobVacancyHandler(IJobVacancyRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(DeleteJobVacancyCommand request, CancellationToken cancellationToken)
    {
      await _repo.DeleteAsync(request.Id, cancellationToken);
    }
  }
}
