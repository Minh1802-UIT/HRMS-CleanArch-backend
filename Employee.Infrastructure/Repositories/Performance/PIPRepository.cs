using Employee.Domain.Entities.Performance;
using Employee.Domain.Enums;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;
using MongoDB.Driver;

namespace Employee.Infrastructure.Repositories.Performance
{
  public class PIPRepository : BaseRepository<PIP>, IPIPRepository
  {
    public PIPRepository(IMongoContext context) : base(context, "performance_improvement_plans") { }

    public async Task<IEnumerable<PIP>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
      var filter = Builders<PIP>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PIP>(),
          Builders<PIP>.Filter.Ne(x => x.Status, Domain.Enums.PIPStatus.Completed),
          Builders<PIP>.Filter.Ne(x => x.Status, Domain.Enums.PIPStatus.Cancelled));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PIP>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PIP>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PIP>(),
          Builders<PIP>.Filter.Eq(x => x.EmployeeId, employeeId));
      return await _collection.Find(filter)
          .SortByDescending(x => x.CreatedAt)
          .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PIP>> GetByManagerIdAsync(string managerId, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PIP>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PIP>(),
          Builders<PIP>.Filter.Eq(x => x.ManagerId, managerId));
      return await _collection.Find(filter)
          .SortByDescending(x => x.CreatedAt)
          .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PIP>> GetByStatusAsync(int status, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PIP>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PIP>(),
          Builders<PIP>.Filter.Eq(x => x.Status, (Domain.Enums.PIPStatus)status));
      return await _collection.Find(filter)
          .SortByDescending(x => x.CreatedAt)
          .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<PIP>> GetOverdueAsync(CancellationToken cancellationToken = default)
    {
      var now = DateTime.UtcNow.Date;
      var filter = Builders<PIP>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PIP>(),
          Builders<PIP>.Filter.Eq(x => x.Status, Domain.Enums.PIPStatus.InProgress),
          Builders<PIP>.Filter.Lt(x => x.EndDate, now));
      return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> CountByStatusAsync(PIPStatus status, CancellationToken cancellationToken = default)
    {
      var filter = Builders<PIP>.Filter.And(
          SoftDeleteFilter.GetActiveOnlyFilter<PIP>(),
          Builders<PIP>.Filter.Eq(x => x.Status, status));
      return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
  }
}
