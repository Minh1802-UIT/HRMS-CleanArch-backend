using Employee.Application.Common.Security;
using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceReview
{
  [Authorize(Roles = "Admin,HR,Manager")]
public record CreatePerformanceReviewCommand(PerformanceReviewDto Dto) : IRequest<string>;
}
