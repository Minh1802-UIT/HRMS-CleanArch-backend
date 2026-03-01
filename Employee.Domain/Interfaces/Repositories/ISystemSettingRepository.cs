using System.Threading;
using Employee.Domain.Entities.Common;
using System.Collections.Generic;

namespace Employee.Domain.Interfaces.Repositories
{
  public interface ISystemSettingRepository
  {
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<Dictionary<string, string>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemSetting>> GetByGroupAsync(string group, CancellationToken cancellationToken = default);
    Task<IEnumerable<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(SystemSetting setting, CancellationToken cancellationToken = default);
    Task CreateAsync(SystemSetting setting, CancellationToken cancellationToken = default);
    Task ClearAllAsync(CancellationToken cancellationToken = default);
  }
}
