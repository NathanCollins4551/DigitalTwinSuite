using DigitalTwin.Application.Abstractions.Telemetry;
using DigitalTwin.Application.Telemetry.Models;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DigitalTwin.Api.Streaming;

public class InMemoryTelemetryPublisher : IPrinterTelemetryPublisher
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<PrinterTelemetryPoint>>> _subscribers = new();

    public ChannelReader<PrinterTelemetryPoint> Subscribe(string deviceId, CancellationToken cancellationToken = default)
    {
        var channel = Channel.CreateUnbounded<PrinterTelemetryPoint>();
        var subscriberId = Guid.NewGuid();

        var deviceSubscribers = _subscribers.GetOrAdd(
            deviceId,
            _ => new ConcurrentDictionary<Guid, Channel<PrinterTelemetryPoint>>());

        deviceSubscribers[subscriberId] = channel;

        cancellationToken.Register(() =>
        {
            UnsubscribeInternal(deviceId, subscriberId, channel);
        });

        return channel.Reader;
    }

    public void Publish(PrinterTelemetryPoint point)
    {
        if (!_subscribers.TryGetValue(point.DeviceId, out var subscribers))
            return;

        foreach (var kvp in subscribers)
        {
            kvp.Value.Writer.TryWrite(point);
        }
    }

    private void UnsubscribeInternal(
        string deviceId,
        Guid subscriberId,
        Channel<PrinterTelemetryPoint> channel)
    {
        if (_subscribers.TryGetValue(deviceId, out var subscribers))
        {
            subscribers.TryRemove(subscriberId, out _);

            if (subscribers.IsEmpty)
            {
                _subscribers.TryRemove(deviceId, out _);
            }
        }

        channel.Writer.TryComplete();
    }
}