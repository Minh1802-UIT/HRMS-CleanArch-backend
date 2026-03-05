using Employee.Application.Common.Exceptions;
using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Interview.UpdateInterview
{
  public class UpdateInterviewHandler : IRequestHandler<UpdateInterviewCommand>
  {
    private readonly IInterviewRepository _repo;

    public UpdateInterviewHandler(IInterviewRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(UpdateInterviewCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Interview with ID {request.Id} not found.");

      // TODO: Add UpdateSchedule(scheduledTime, location) to Interview domain entity,
      // then call entity.UpdateSchedule(request.Dto.ScheduledTime, request.Dto.Location).
      await Task.CompletedTask;
    }
  }
}
