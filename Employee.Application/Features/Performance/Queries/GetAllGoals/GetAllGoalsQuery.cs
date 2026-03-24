using MediatR;
using Employee.Application.Features.Performance.Dtos;
using System.Collections.Generic;

namespace Employee.Application.Features.Performance.Queries.GetAllGoals
{
  /// <summary>
  /// Gets all performance goals across all employees (for HR/Manager dashboard).
  /// </summary>
  public record GetAllGoalsQuery() : IRequest<List<PerformanceGoalResponseDto>>;
}
