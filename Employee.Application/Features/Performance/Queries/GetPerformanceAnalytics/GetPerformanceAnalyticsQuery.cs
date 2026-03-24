using MediatR;
using Employee.Application.Features.Performance.Dtos;

namespace Employee.Application.Features.Performance.Queries.GetPerformanceAnalytics
{
  public record GetPerformanceAnalyticsQuery() : IRequest<PerformanceAnalyticsDto>;
}
