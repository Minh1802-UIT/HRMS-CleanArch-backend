using Employee.Application.Common.Security;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.UpdatePerformanceGoalProgress
{
  [Authorize(Roles = "Admin,HR,Manager")]
public record UpdatePerformanceGoalProgressCommand(string Id, double Progress) : IRequest<bool>;
}
