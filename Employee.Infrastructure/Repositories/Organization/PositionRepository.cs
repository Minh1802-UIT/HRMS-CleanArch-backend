using Employee.Domain.Interfaces.Repositories;
using Employee.Domain.Entities.Organization;
using MongoDB.Driver;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Organization
{
  public class PositionRepository : BaseRepository<Position>, IPositionRepository
  {
    public PositionRepository(IMongoContext context) : base(context, "positions")
    {
    }

    public async Task<Dictionary<string, string>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
      var filter = Builders<Position>.Filter.And(
          Builders<Position>.Filter.In(x => x.Id, ids),
          Builders<Position>.Filter.Ne(x => x.IsDeleted, true)
      );
      var results = await _collection
          .Find(filter)
          .Project(x => new { x.Id, x.Title })
          .ToListAsync(cancellationToken);

      return results.ToDictionary(x => x.Id, x => x.Title);
    }

    public async Task<List<Position>> GetSubordinatesAsync(string parentId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.ParentId == parentId && x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<List<Position>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted != true).ToListAsync(cancellationToken);
    }
  }
}
