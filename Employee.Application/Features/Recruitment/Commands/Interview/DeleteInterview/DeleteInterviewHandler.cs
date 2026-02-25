using Employee.Application.Common.Interfaces.Organization.IRepository;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Interview.DeleteInterview
{
  public class DeleteInterviewHandler : IRequestHandler<DeleteInterviewCommand>
  {
    private readonly IInterviewRepository _repo;

    public DeleteInterviewHandler(IInterviewRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(DeleteInterviewCommand request, CancellationToken cancellationToken)
    {
      await _repo.DeleteAsync(request.Id, cancellationToken);
    }
  }
}
