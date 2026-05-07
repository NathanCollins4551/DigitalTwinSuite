using DigitalTwin.Application.Abstractions.Caching;
using StackExchange.Redis;

namespace DigitalTwin.Infrastructure.Caching;

public class RedisFleetCache : IFleetCache
{
    private readonly IDatabase _database;

    public RedisFleetCache(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<string?> GetSignatureAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await _database.StringGetAsync(key);
        return value.HasValue ? value.ToString() : null;
    }

    public async Task SetSignatureAsync(string key, string signature, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        await _database.StringSetAsync(key, signature, ttl);
    }
}