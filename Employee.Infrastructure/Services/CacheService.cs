using Employee.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Employee.Infrastructure.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;

        /// <summary>
        /// Hard deadline for any single Redis call.
        /// StackExchange.Redis ignores CancellationToken while a request is queued
        /// in the backlog (waiting for a cold connection to be established), so we
        /// enforce the limit ourselves with Task.WhenAny + Task.Delay.
        /// Set comfortably below the 5 s StackExchange internal backlog timeout so
        /// we never block a request for more than ~1.5 s even when Redis is asleep.
        /// </summary>
        private static readonly TimeSpan _timeout = TimeSpan.FromMilliseconds(1400);

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var task = _cache.GetStringAsync(key);
                if (await Task.WhenAny(task, Task.Delay(_timeout)) != task)
                {
                    _logger.LogWarning("Redis cache GetAsync timed out for key: {Key}", key);
                    return default;   // treat as cache miss — fall through to DB
                }
                var jsonData = await task;
                if (jsonData == null) return default;
                return JsonSerializer.Deserialize<T>(jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache GetAsync failed for key: {Key}", key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromHours(1)
                };
                var jsonData = JsonSerializer.Serialize(value);
                var task = _cache.SetStringAsync(key, jsonData, options);
                if (await Task.WhenAny(task, Task.Delay(_timeout)) != task)
                {
                    _logger.LogWarning("Redis cache SetAsync timed out for key: {Key}", key);
                    return;   // non-critical — the response is already on its way to the client
                }
                await task;   // propagate any exception from the completed task
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache SetAsync failed for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                var task = _cache.RemoveAsync(key);
                if (await Task.WhenAny(task, Task.Delay(_timeout)) != task)
                {
                    _logger.LogWarning("Redis cache RemoveAsync timed out for key: {Key}", key);
                    return;
                }
                await task;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis cache RemoveAsync failed for key: {Key}", key);
            }
        }
    }
}
