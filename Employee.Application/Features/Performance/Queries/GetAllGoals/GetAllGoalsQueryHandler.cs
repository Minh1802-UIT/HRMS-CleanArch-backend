using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Enums;
using Employee.Application.Features.Performance.Dtos;
using Employee.Application.Features.Performance.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Queries.GetAllGoals
{
  public class GetAllGoalsQueryHandler : IRequestHandler<GetAllGoalsQuery, List<PerformanceGoalResponseDto>>
  {
    private readonly IPerformanceGoalRepository _repo;

    public GetAllGoalsQueryHandler(IPerformanceGoalRepository repo)
    {
      _repo = repo;
    }

    public async Task<List<PerformanceGoalResponseDto>> Handle(GetAllGoalsQuery request, CancellationToken cancellationToken)
    {
      var all = await _repo.GetAllAsync(cancellationToken);
      var goalList = all.ToList();

      // Auto-mark overdue
      foreach (var goal in goalList)
      {
        goal.MarkAsOverdueIfPastDue();
        if (goal.Status == PerformanceGoalStatus.Overdue)
        {
          await _repo.UpdateAsync(goal.Id, goal, cancellationToken);
        }
      }

      return goalList.Select(g => g.ToDto()).ToList();
    }
  }
}
