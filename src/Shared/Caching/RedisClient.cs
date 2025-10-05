using StackExchange.Redis;
using System.Text.Json;

namespace Shared.Caching;

public class RedisClient : IRedisClient, IDisposable
{
    private readonly ConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private static readonly string[] HostFallbackOrder = [
        "redis",
        "localhost"
    ];

    public RedisClient(string? connectionString = null)
    {
        var envConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION");
        var envHost = Environment.GetEnvironmentVariable("REDIS_HOST");
        string baseConn = connectionString
                           ?? envConn
                           ?? (envHost is not null ? $"{envHost}:6379" : null)
                           ?? "redis:6379";

        if (!baseConn.Contains("abortConnect", StringComparison.OrdinalIgnoreCase))
        {
            baseConn += ",abortConnect=false";
        }
        if (!baseConn.Contains("connectRetry", StringComparison.OrdinalIgnoreCase))
        {
            baseConn += ",connectRetry=5";
        }
        if (!baseConn.Contains("connectTimeout", StringComparison.OrdinalIgnoreCase))
        {
            baseConn += ",connectTimeout=5000";
        }

        ConnectionMultiplexer? mux = null;
        Exception? last = null;
        var attempted = new List<string>();

        var parts = baseConn.Split(',')[0]; // first segment contains host:port
        var hostFromBase = parts.Contains(':') ? parts.Split(':')[0] : parts;
        var sequence = new List<string> { hostFromBase };
        sequence.AddRange(HostFallbackOrder.Where(h => h != hostFromBase));

        foreach (var host in sequence.Distinct())
        {
            var hostConn = System.Text.RegularExpressions.Regex.Replace(baseConn, "^[^,]+", _ => host + ":6379");
            attempted.Add(hostConn);
            try
            {
                Console.WriteLine($"[RedisClient] Attempting connection: {hostConn}");
                mux = ConnectionMultiplexer.Connect(hostConn);
                if (host != hostFromBase)
                    Console.WriteLine($"[RedisClient] Fallback host succeeded: {host}");
                last = null;
                break;
            }
            catch (Exception ex)
            {
                last = ex;
                Console.Error.WriteLine($"[RedisClient] Connection attempt failed: {hostConn} -> {ex.Message}");
            }
        }

        if (mux == null)
        {
            Console.Error.WriteLine($"[RedisClient] All connection attempts failed. Tried: {string.Join(" | ", attempted)}");
            throw last ?? new InvalidOperationException("Unable to connect to Redis.");
        }

        _redis = mux;
        _db = _redis.GetDatabase();
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var json = JsonSerializer.Serialize(value);
        await _db.StringSetAsync(key, json, expiry);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _db.StringGetAsync(key);
        return json.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(json!);
    }

    public void Dispose() => _redis?.Dispose();
}
