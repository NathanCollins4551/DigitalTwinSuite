namespace DigitalTwin.Application.Abstractions.Caching;

public interface IFleetCache
{
    Task<string?> GetSignatureAsync(string key, CancellationToken cancellationToken = default);
    Task SetSignatureAsync(string key, string signature, TimeSpan ttl, CancellationToken cancellationToken = default);
}