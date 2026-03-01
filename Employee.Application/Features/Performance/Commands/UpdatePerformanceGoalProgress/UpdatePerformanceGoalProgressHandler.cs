using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.UpdatePerformanceGoalProgress
{
  public class UpdatePerformanceGoalProgressHandler : IRequestHandler<UpdatePerformanceGoalProgressCommand, bool>
  {
    private readonly IPerformanceGoalRepository _repo;

    public UpdatePerformanceGoalProgressHandler(IPerformanceGoalRepository repo)
    {
      _repo = repo;
    }

    public async Task<bool> Handle(UpdatePerformanceGoalProgressCommand request, CancellationToken cancellationToken)
    {
      var goal = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (goal == null) return false;

      goal.UpdateProgress(request.Progress);
      await _repo.UpdateAsync(goal.Id, goal, cancellationToken);
      return true;
    }
  }
}
