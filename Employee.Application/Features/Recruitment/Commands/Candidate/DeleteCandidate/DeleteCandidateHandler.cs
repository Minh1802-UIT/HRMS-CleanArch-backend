using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.DeleteCandidate
{
  public class DeleteCandidateHandler : IRequestHandler<DeleteCandidateCommand>
  {
    private readonly ICandidateRepository _repo;

    public DeleteCandidateHandler(ICandidateRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(DeleteCandidateCommand request, CancellationToken cancellationToken)
    {
      await _repo.DeleteAsync(request.Id, cancellationToken);
    }
  }
}
