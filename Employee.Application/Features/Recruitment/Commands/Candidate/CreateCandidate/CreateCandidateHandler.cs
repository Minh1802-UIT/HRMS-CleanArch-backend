using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.CreateCandidate
{
  public class CreateCandidateHandler : IRequestHandler<CreateCandidateCommand>
  {
    private readonly ICandidateRepository _repo;

    public CreateCandidateHandler(ICandidateRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(CreateCandidateCommand request, CancellationToken cancellationToken)
    {
      var entity = request.Dto.ToEntity();
      await _repo.CreateAsync(entity, cancellationToken);
    }
  }
}
