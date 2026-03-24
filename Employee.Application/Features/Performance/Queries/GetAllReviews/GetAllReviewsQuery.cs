using MediatR;
using Employee.Application.Features.Performance.Dtos;
using System.Collections.Generic;

namespace Employee.Application.Features.Performance.Queries.GetAllReviews
{
  /// <summary>
  /// Gets all performance reviews across all employees (for HR/Manager dashboard).
  /// </summary>
  public record GetAllReviewsQuery() : IRequest<List<PerformanceReviewResponseDto>>;
}
