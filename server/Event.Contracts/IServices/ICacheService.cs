using System;
using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface ICacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);
    }
}
