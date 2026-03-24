using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Enums;
using Employee.Application.Features.Performance.Dtos;
using Employee.Application.Features.Performance.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Queries.GetEmployeeGoals
{
  public class GetEmployeeGoalsQueryHandler : IRequestHandler<GetEmployeeGoalsQuery, List<PerformanceGoalResponseDto>>
  {
    private readonly IPerformanceGoalRepository _repo;

    public GetEmployeeGoalsQueryHandler(IPerformanceGoalRepository repo)
    {
      _repo = repo;
    }

    public async Task<List<PerformanceGoalResponseDto>> Handle(GetEmployeeGoalsQuery request, CancellationToken cancellationToken)
    {
      var goals = await _repo.GetByEmployeeIdAsync(request.EmployeeId, cancellationToken);
      var dtos = new List<PerformanceGoalResponseDto>();

      foreach (var goal in goals)
      {
        goal.MarkAsOverdueIfPastDue();
        dtos.Add(goal.ToDto());

        // Persist overdue status change so it survives across API calls
        if (goal.Status == PerformanceGoalStatus.Overdue)
        {
          await _repo.UpdateAsync(goal.Id, goal, cancellationToken);
        }
      }

      return dtos;
    }
  }
}
