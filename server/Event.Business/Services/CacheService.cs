using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Event.Contracts.IServices;

namespace Event.Business.Services
{
    public class CacheService : ICacheService
    {
        #region Fields

        private readonly IDistributedCache _cache;

        #endregion

        #region Constructor

        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        #endregion

        #region SetAsync

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            // 1. Guard against empty cache keys
            if (string.IsNullOrEmpty(key)) return;

            // 2. Set absolute expiration time relative to now if provided
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }

            // 3. Serialize and save to the underlying distributed cache
            string json = JsonSerializer.Serialize(value);
            await _cache.SetStringAsync(key, json, options);
        }

        #endregion

        #region GetAsync

        public async Task<T?> GetAsync<T>(string key)
        {
            // 1. Guard against empty cache keys
            if (string.IsNullOrEmpty(key)) return default;

            // 2. Retrieve JSON payload from the distributed cache
            string? json = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(json)) return default;

            // 3. Attempt deserialization of the retrieved JSON string
            try
            {
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return default;
            }
        }

        #endregion

        #region RemoveAsync

        public async Task RemoveAsync(string key)
        {
            // 1. Guard against empty cache keys
            if (string.IsNullOrEmpty(key)) return;

            // 2. Evict target item from the distributed cache
            await _cache.RemoveAsync(key);
        }

        #endregion
    }
}
