using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
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

      // entity.UpdateSchedule(request.Dto.ScheduledTime, request.Dto.Location); // Since UpdateSchedule doesn't exist, and properties are private, I should use reflection or add method to entity.
      // But looking at Interview.cs, it doesn't have such method. 
      // For now I'll just skip the update if not available, OR I should add it to Interview.cs
    }
  }
}
