using Employee.Application.Features.Performance.Dtos;
using MediatR;
using System.Collections.Generic;

namespace Employee.Application.Features.Performance.Queries.GetEmployeeReviews
{
  public record GetEmployeeReviewsQuery(string EmployeeId) : IRequest<List<PerformanceReviewResponseDto>>;
}
