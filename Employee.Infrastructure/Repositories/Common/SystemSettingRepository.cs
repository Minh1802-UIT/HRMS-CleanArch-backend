using Employee.Application.Common.Interfaces.Organization.IRepository;
using Employee.Domain.Entities.Common;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using Employee.Infrastructure.Persistence;

namespace Employee.Infrastructure.Repositories.Common
{
  public class SystemSettingRepository : ISystemSettingRepository
  {
    private readonly IMongoCollection<SystemSetting> _collection;
    private readonly IMongoContext _context;

    public SystemSettingRepository(IMongoContext context)
    {
      _context = context;
      _collection = _context.GetCollection<SystemSetting>("system_settings");
    }

    public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.Key == key).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
      var filter = Builders<SystemSetting>.Filter.In(x => x.Key, keys);
      var results = await _collection.Find(filter)
          .Project(x => new { x.Key, x.Value })
          .ToListAsync(cancellationToken);
      return results.ToDictionary(x => x.Key, x => x.Value);
    }

    public async Task<IEnumerable<SystemSetting>> GetByGroupAsync(string group, CancellationToken cancellationToken = default)
    {
      return await _collection.Find(x => x.Group == group).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
    {
      return await _collection.Find(_ => true).ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
      var filter = Builders<SystemSetting>.Filter.Eq(x => x.Key, setting.Key);
      var update = Builders<SystemSetting>.Update
          .Set(x => x.Value, setting.Value)
          .Set(x => x.Description, setting.Description)
          .Set(x => x.Group, setting.Group)
          .SetOnInsert(x => x.CreatedAt, DateTime.UtcNow)
          .SetOnInsert(x => x.IsDeleted, false)
          .SetOnInsert(x => x.Version, 1);

      await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, cancellationToken);
    }

    public async Task CreateAsync(SystemSetting setting, CancellationToken cancellationToken = default)
    {
      await _collection.InsertOneAsync(setting, cancellationToken: cancellationToken);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
      await _collection.DeleteManyAsync(_ => true, cancellationToken);
    }
  }
}
