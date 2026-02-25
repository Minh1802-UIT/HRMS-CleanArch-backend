using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Recruitment.Mappers;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Recruitment.Commands.Interview.CreateInterview
{
  public class CreateInterviewHandler : IRequestHandler<CreateInterviewCommand>
  {
    private readonly IInterviewRepository _repo;

    public CreateInterviewHandler(IInterviewRepository repo)
    {
      _repo = repo;
    }

    public async Task Handle(CreateInterviewCommand request, CancellationToken cancellationToken)
    {
      var entity = request.Dto.ToEntity();
      await _repo.CreateAsync(entity, cancellationToken);
    }
  }
}
