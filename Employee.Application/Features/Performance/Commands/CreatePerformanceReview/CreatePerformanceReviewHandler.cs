using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Performance;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.CreatePerformanceReview
{
  public class CreatePerformanceReviewHandler : IRequestHandler<CreatePerformanceReviewCommand, string>
  {
    private readonly IPerformanceReviewRepository _repo;

    public CreatePerformanceReviewHandler(IPerformanceReviewRepository repo)
    {
      _repo = repo;
    }

    public async Task<string> Handle(CreatePerformanceReviewCommand request, CancellationToken cancellationToken)
    {
      var review = new PerformanceReview(
        request.Dto.EmployeeId,
        request.Dto.ReviewerId,
        request.Dto.PeriodStart,
        request.Dto.PeriodEnd
      );

      await _repo.CreateAsync(review, cancellationToken);
      return review.Id;
    }
  }
}
