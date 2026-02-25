using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.UpdatePerformanceReview
{
  public record UpdatePerformanceReviewCommand(string Id, PerformanceReviewDto Dto) : IRequest<bool>;
}
