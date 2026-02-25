using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Interview.ReviewInterview
{
  public class ReviewInterviewHandler : IRequestHandler<ReviewInterviewCommand>
  {
    private readonly IInterviewRepository _repo;

    public ReviewInterviewHandler(IInterviewRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(ReviewInterviewCommand request, CancellationToken cancellationToken)
    {
      var entity = await _repo.GetByIdAsync(request.Id, cancellationToken)
          ?? throw new NotFoundException($"Interview with ID {request.Id} not found.");

      if (Enum.TryParse<InterviewStatus>(request.Dto.Result, true, out var status))
      {
        if (status == InterviewStatus.Completed)
        {
          entity.Complete(request.Dto.Notes);
        }
        else if (status == InterviewStatus.Cancelled)
        {
          entity.Cancel();
        }
      }
      else
      {
        throw new ValidationException($"Invalid status: {request.Dto.Result}");
      }

      await _repo.UpdateAsync(entity, cancellationToken);
    }
  }
}
