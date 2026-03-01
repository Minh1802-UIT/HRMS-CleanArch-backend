using Employee.Application.Common.Security;
using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceGoal
{
  [Authorize(Roles = "Admin,HR,Manager")]
public record CreatePerformanceGoalCommand(PerformanceGoalDto Dto) : IRequest<string>;
}
