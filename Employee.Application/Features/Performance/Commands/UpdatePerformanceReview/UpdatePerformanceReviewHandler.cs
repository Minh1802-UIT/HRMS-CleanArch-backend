using Employee.Domain.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.Application.Features.Performance.Commands.UpdatePerformanceReview
{
  public class UpdatePerformanceReviewHandler : IRequestHandler<UpdatePerformanceReviewCommand, bool>
  {
    private readonly IPerformanceReviewRepository _repo;

    public UpdatePerformanceReviewHandler(IPerformanceReviewRepository repo)
    {
      _repo = repo;
    }

    public async Task<bool> Handle(UpdatePerformanceReviewCommand request, CancellationToken cancellationToken)
    {
      var review = await _repo.GetByIdAsync(request.Id, cancellationToken);
      if (review == null) return false;

      review.UpdateReview(request.Dto.OverallScore, request.Dto.Notes, request.Dto.Status);
      await _repo.UpdateAsync(review.Id, review, cancellationToken);
      return true;
    }
  }
}
