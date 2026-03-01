using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using MediatR;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidate
{
  public class UpdateCandidateHandler : IRequestHandler<UpdateCandidateCommand>
  {
    private readonly ICandidateRepository _repo;

    public UpdateCandidateHandler(ICandidateRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(UpdateCandidateCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Candidate with ID {request.Id} not found.");

      entity.UpdateInfo(request.Dto.FullName, request.Dto.Email, request.Dto.Phone ?? string.Empty);
      entity.UpdateResume(request.Dto.ResumeUrl ?? string.Empty);

      await _repo.UpdateAsync(entity.Id, entity, cancellationToken);
    }
  }
}
