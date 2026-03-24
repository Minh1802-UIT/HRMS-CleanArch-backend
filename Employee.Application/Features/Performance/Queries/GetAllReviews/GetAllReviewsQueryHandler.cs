using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Features.Performance.Dtos;
using Employee.Application.Features.Performance.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Queries.GetAllReviews
{
  public class GetAllReviewsQueryHandler : IRequestHandler<GetAllReviewsQuery, List<PerformanceReviewResponseDto>>
  {
    private readonly IPerformanceReviewRepository _reviewRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetAllReviewsQueryHandler(IPerformanceReviewRepository reviewRepo, IEmployeeRepository employeeRepo)
    {
      _reviewRepo = reviewRepo;
      _employeeRepo = employeeRepo;
    }

    public async Task<List<PerformanceReviewResponseDto>> Handle(GetAllReviewsQuery request, CancellationToken cancellationToken)
    {
      var reviews = await _reviewRepo.GetAllAsync(cancellationToken);
      var reviewList = reviews.ToList();

      if (!reviewList.Any())
        return new List<PerformanceReviewResponseDto>();

      var allIds = reviewList.SelectMany(r => new[] { r.EmployeeId, r.ReviewerId }).Distinct().ToList();
      var names = await _employeeRepo.GetNamesByIdsAsync(allIds, cancellationToken);

      return reviewList.Select(review =>
        review.ToDto(
          names.TryGetValue(review.EmployeeId, out var emp) ? emp.Name : "Unknown",
          names.TryGetValue(review.ReviewerId, out var rev) ? rev.Name : "Unknown"
        )
      ).ToList();
    }
  }
}
