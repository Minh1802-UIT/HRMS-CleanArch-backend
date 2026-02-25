using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceGoal
{
  public record CreatePerformanceGoalCommand(PerformanceGoalDto Dto) : IRequest<string>;
}
