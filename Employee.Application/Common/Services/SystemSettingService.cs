using Employee.Domain.Interfaces.Repositories;
using Employee.Application.Common.Interfaces.Organization.IService;
using Employee.Application.Common.Interfaces; // For ICacheService
using Employee.Domain.Entities.Common;
using System.Globalization;

namespace Employee.Application.Common.Services
{
  public class SystemSettingService : ISystemSettingService
  {
    private readonly ISystemSettingRepository _repo;
    private readonly ICacheService _cache;
    private readonly Employee.Domain.Interfaces.Common.IDateTimeProvider _dateTime;

    public SystemSettingService(ISystemSettingRepository repo, ICacheService cache, Employee.Domain.Interfaces.Common.IDateTimeProvider dateTime)
    {
      _repo = repo;
      _cache = cache;
      _dateTime = dateTime;
    }

    public async Task<string> GetStringAsync(string key, string defaultValue = "")
    {
      string cacheKey = $"SYS_SETTING_{key}";
      var cachedValue = await _cache.GetAsync<string>(cacheKey);
      if (cachedValue != null) return cachedValue;

      var setting = await _repo.GetByKeyAsync(key);
      var value = setting?.Value ?? defaultValue;

      if (setting != null)
      {
        await _cache.SetAsync(cacheKey, value, TimeSpan.FromMinutes(30));
      }
      return value;
    }

    public async Task<Dictionary<string, string>> GetMultipleAsync(IEnumerable<string> keys)
    {
      // P3-FIX: Leverage cached GetStringAsync for each key
      var result = new Dictionary<string, string>();
      foreach (var key in keys)
      {
        result[key] = await GetStringAsync(key);
      }
      return result;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0)
    {
      var value = await GetStringAsync(key);
      if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
      {
        return result;
      }
      return defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
      var value = await GetStringAsync(key);
      if (int.TryParse(value, out var result))
      {
        return result;
      }
      return defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
      var value = await GetStringAsync(key);
      if (bool.TryParse(value, out var result))
      {
        return result;
      }
      return defaultValue;
    }

    public async Task SetAsync(string key, string value, string group = "General", string description = "")
    {
      var setting = await _repo.GetByKeyAsync(key) ?? new SystemSetting(key, group);

      setting.UpdateValue(value, description, _dateTime.UtcNow);

      await _repo.UpsertAsync(setting);

      // Invalidate Cache
      string cacheKey = $"SYS_SETTING_{key}";
      await _cache.RemoveAsync(cacheKey);
    }
  }
}

