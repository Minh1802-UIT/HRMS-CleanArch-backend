using Employee.Application.Features.Performance.Dtos;
using MediatR;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceReview
{
  public record CreatePerformanceReviewCommand(PerformanceReviewDto Dto) : IRequest<string>;
}
