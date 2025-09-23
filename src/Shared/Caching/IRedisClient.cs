namespace Shared.Caching;

public interface IRedisClient
{
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
}
