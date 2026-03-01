using Employee.Application.Common.Security;
using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.UpdatePerformanceReview
{
  [Authorize(Roles = "Admin,HR,Manager")]
public record UpdatePerformanceReviewCommand(string Id, PerformanceReviewDto Dto) : IRequest<bool>;
}
