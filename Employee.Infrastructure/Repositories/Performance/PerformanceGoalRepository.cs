using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Performance;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Performance
{
  public class PerformanceGoalRepository : BaseRepository<PerformanceGoal>, IPerformanceGoalRepository
  {
    public PerformanceGoalRepository(IMongoContext context) : base(context, "performance_goals")
    {
    }

    public async Task<IEnumerable<PerformanceGoal>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PerformanceGoal>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PerformanceGoal>(),
          Builders<PerformanceGoal>.Filter.Eq(x => x.EmployeeId, employeeId));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
  }
}
