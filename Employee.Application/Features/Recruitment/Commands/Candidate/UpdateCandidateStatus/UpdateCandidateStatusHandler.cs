using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Candidate.UpdateCandidateStatus
{
  public class UpdateCandidateStatusHandler : IRequestHandler<UpdateCandidateStatusCommand>
  {
    private readonly ICandidateRepository _repo;

    public UpdateCandidateStatusHandler(ICandidateRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(UpdateCandidateStatusCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Candidate with ID {request.Id} not found.");

      if (Enum.TryParse<CandidateStatus>(request.Status, true, out var domainStatus))
      {
        entity.UpdateStatus(domainStatus);
      }
      else
      {
        throw new ValidationException($"Invalid status: {request.Status}");
      }

      await _repo.UpdateAsync(entity, cancellationToken);
    }
  }
}
