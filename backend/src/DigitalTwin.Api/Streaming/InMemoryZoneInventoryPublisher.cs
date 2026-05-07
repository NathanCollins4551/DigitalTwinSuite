using DigitalTwin.Application.Abstractions.Inventory;
using DigitalTwin.Application.Inventory.Dtos;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DigitalTwin.Api.Streaming;

public class InMemoryZoneInventoryPublisher : IZoneInventoryPublisher
{
    private readonly ConcurrentDictionary<Guid, Channel<ZoneSnapshotResponseDto>> _subscribers = new();

    public ChannelReader<ZoneSnapshotResponseDto> Subscribe(CancellationToken cancellationToken = default)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateUnbounded<ZoneSnapshotResponseDto>();

        _subscribers[subscriberId] = channel;

        cancellationToken.Register(() =>
        {
            if (_subscribers.TryRemove(subscriberId, out var removed))
            {
                removed.Writer.TryComplete();
            }
        });

        return channel.Reader;
    }

    public void Publish(ZoneSnapshotResponseDto snapshot)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(snapshot);
        }
    }
}