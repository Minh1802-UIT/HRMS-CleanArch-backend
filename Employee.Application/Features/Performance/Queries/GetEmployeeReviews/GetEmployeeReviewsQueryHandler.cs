using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Application.Features.Performance.Dtos;
using Employee.Application.Features.Performance.Mappers;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Queries.GetEmployeeReviews
{
  public class GetEmployeeReviewsQueryHandler : IRequestHandler<GetEmployeeReviewsQuery, List<PerformanceReviewResponseDto>>
  {
    private readonly IPerformanceReviewRepository _reviewRepo;
    private readonly IEmployeeRepository _employeeRepo;

    public GetEmployeeReviewsQueryHandler(IPerformanceReviewRepository reviewRepo, IEmployeeRepository employeeRepo)
    {
      _reviewRepo = reviewRepo;
      _employeeRepo = employeeRepo;
    }

    public async Task<List<PerformanceReviewResponseDto>> Handle(GetEmployeeReviewsQuery request, CancellationToken cancellationToken)
    {
      var reviews = await _reviewRepo.GetByEmployeeIdAsync(request.EmployeeId, cancellationToken);
      var result = new List<PerformanceReviewResponseDto>();

      foreach (var review in reviews)
      {
        var employee = await _employeeRepo.GetByIdAsync(review.EmployeeId, cancellationToken);
        var reviewer = await _employeeRepo.GetByIdAsync(review.ReviewerId, cancellationToken);
        result.Add(review.ToDto(employee?.FullName ?? "Unknown", reviewer?.FullName ?? "Unknown"));
      }

      return result;
    }
  }
}
