using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Performance;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceGoal
{
  public class CreatePerformanceGoalHandler : IRequestHandler<CreatePerformanceGoalCommand, string>
  {
    private readonly IPerformanceGoalRepository _repo;

    public CreatePerformanceGoalHandler(IPerformanceGoalRepository repo)
    {
      _repo = repo;
    }

    public async Task<string> Handle(CreatePerformanceGoalCommand request, CancellationToken cancellationToken)
    {
      var goal = new PerformanceGoal(
        request.Dto.EmployeeId,
        request.Dto.Title,
        request.Dto.Description,
        request.Dto.TargetDate
      );

      await _repo.CreateAsync(goal, cancellationToken);
      return goal.Id;
    }
  }
}
