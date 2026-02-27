using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Performance;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Performance
{
  public class PerformanceReviewRepository : BaseRepository<PerformanceReview>, IPerformanceReviewRepository
  {
    public PerformanceReviewRepository(IMongoContext context) : base(context, "performance_reviews")
    {
    }

    public async Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PerformanceReview>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceReview>(),
          Builders<PerformanceReview>.Filter.Eq(x => x.EmployeeId, employeeId));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
  }
}
