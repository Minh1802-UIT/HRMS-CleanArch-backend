using Employee.Domain.Entities.Organization;
using MongoDB.Driver;
using Employee.Domain.Interfaces.Repositories;
using Employee.Infrastructure.Persistence;
using Employee.Infrastructure.Repositories.Common;

namespace Employee.Infrastructure.Repositories.Organization
{
  public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
  {
    public DepartmentRepository(IMongoContext context) : base(context, "departments")
    {
    }

    public async Task<Dictionary<string, string>> GetNamesByIdsAsync(List<string> ids, CancellationToken cancellationToken = default)
    {
      var filter = Builders<Department>.Filter.And(
          Builders<Department>.Filter.In(x => x.Id, ids),
          Builders<Department>.Filter.Eq(x => x.IsDeleted, false)
      );
      var results = await _collection
          .Find(filter)
          .Project(x => new { x.Id, x.Name })
          .ToListAsync(cancellationToken);

      return results.ToDictionary(x => x.Id, x => x.Name);
    }

    public async Task<bool> ExistsByManagerIdAsync(string managerId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.ManagerId == managerId && x.IsDeleted != true).AnyAsync(cancellationToken);
    }

    public async Task<List<Department>> GetChildrenAsync(string parentId, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.ParentId == parentId && x.IsDeleted != true).ToListAsync(cancellationToken);
    }

    public async Task<List<Department>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.IsDeleted != true).ToListAsync(cancellationToken);
    }
  }
}
