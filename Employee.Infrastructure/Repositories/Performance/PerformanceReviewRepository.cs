using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Performance;
using Employee.Infrastructure.Persistence;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Employee.Infrastructure.Repositories.Performance
{
  public class PerformanceReviewRepository : IPerformanceReviewRepository
  {
    private readonly IMongoCollection<PerformanceReview> _collection;

    public PerformanceReviewRepository(IMongoContext context)
    {
      _collection = context.GetCollection<PerformanceReview>("performance_reviews");
    }

    public async Task<IEnumerable<PerformanceReview>> GetAllAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<PerformanceReview?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.Id == id && x.IsDeleted != true).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<PerformanceReview>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.EmployeeId == employeeId && x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(PerformanceReview entity, CancellationToken cancellationToken = default)
    {
      await _collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
    }

    public async Task UpdateAsync(PerformanceReview entity, CancellationToken cancellationToken = default)
    {
      await _collection.ReplaceOneAsync(x => x.Id == entity.Id, entity, cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
      var update = Builders<PerformanceReview>.Update
          .Set(x => x.IsDeleted, true);
      await _collection.UpdateOneAsync(x => x.Id == id, update, cancellationToken: cancellationToken);
    }
  }
}
