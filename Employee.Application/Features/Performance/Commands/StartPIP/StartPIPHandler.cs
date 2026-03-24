using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.StartPIP
{
  public class StartPIPHandler : IRequestHandler<StartPIPCommand, bool>
  {
    private readonly IPIPRepository _repo;

    public StartPIPHandler(IPIPRepository repo)
    {
      _repo = repo;
    }

    public async Task<bool> Handle(StartPIPCommand request, CancellationToken cancellationToken)
    {
      var pip = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (pip == null) return false;

      pip.Start();
      await _repo.UpdateAsync(pip.Id, pip, cancellationToken);
      return true;
    }
  }
}
