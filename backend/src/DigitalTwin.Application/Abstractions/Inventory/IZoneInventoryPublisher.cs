using DigitalTwin.Application.Inventory.Dtos;
using System.Threading.Channels;

namespace DigitalTwin.Application.Abstractions.Inventory;

public interface IZoneInventoryPublisher
{
    ChannelReader<ZoneSnapshotResponseDto> Subscribe(CancellationToken cancellationToken = default);
    void Publish(ZoneSnapshotResponseDto snapshot);
}