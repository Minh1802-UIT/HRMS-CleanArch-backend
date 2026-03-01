using Employee.Domain.Interfaces.Repositories;
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
      return goals.Select(g => g.ToDto()).ToList();
    }
  }
}
