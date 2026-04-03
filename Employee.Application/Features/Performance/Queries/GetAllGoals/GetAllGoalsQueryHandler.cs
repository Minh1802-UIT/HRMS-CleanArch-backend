using Employee.Domain.Interfaces.Repositories;
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

      // Auto-mark overdue for response only; persistence handled by background job.
      foreach (var goal in goalList)
      {
        goal.MarkAsOverdueIfPastDue();
      }

      return goalList.Select(g => g.ToDto()).ToList();
    }
  }
}
