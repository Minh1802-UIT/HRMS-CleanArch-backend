using Employee.Application.Features.Performance.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Performance.Queries.GetEmployeeGoals
{
  public record GetEmployeeGoalsQuery(string EmployeeId) : IRequest<List<PerformanceGoalResponseDto>>;
}
