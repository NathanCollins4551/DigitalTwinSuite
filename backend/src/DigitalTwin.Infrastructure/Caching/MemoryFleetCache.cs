using DigitalTwin.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace DigitalTwin.Infrastructure.Caching;

public class MemoryFleetCache : IFleetCache
{
    private readonly IMemoryCache _memoryCache;

    public MemoryFleetCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public Task<string?> GetSignatureAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryGetValue(key, out string? value);
        return Task.FromResult(value);
    }

    public Task SetSignatureAsync(string key, string signature, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(key, signature, ttl);
        return Task.CompletedTask;
    }
}