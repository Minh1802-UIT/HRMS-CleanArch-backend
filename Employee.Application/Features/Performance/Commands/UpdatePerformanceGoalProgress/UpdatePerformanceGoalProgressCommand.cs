using MediatR;

namespace Employee.Application.Features.Performance.Commands.UpdatePerformanceGoalProgress
{
  public record UpdatePerformanceGoalProgressCommand(string Id, double Progress) : IRequest<bool>;
}
