using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Performance;
using Employee.Infrastructure.Persistence;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Infrastructure.Repositories.Performance
{
  public class PerformanceGoalRepository : IPerformanceGoalRepository
  {
    private readonly IMongoCollection<PerformanceGoal> _collection;

    public PerformanceGoalRepository(IMongoContext context)
    {
      _collection = context.GetCollection<PerformanceGoal>("performance_goals");
    }

    public async Task<IEnumerable<PerformanceGoal>> GetAllAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<PerformanceGoal?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.Id == id && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PerformanceGoal>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.EmployeeId == employeeId && x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(PerformanceGoal entity, CancellationToken cancellationToken = default)
    {
      await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(PerformanceGoal entity, CancellationToken cancellationToken = default)
    {
      await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
      var update = Builders<PerformanceGoal>.Update
          .Set(x => x.IsDeleted, true);
      await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }
  }
}
