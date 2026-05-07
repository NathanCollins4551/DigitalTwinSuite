using System.Text.Json;
using DigitalTwin.Application.Abstractions.Inventory;
using DigitalTwin.Infrastructure.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace DigitalTwin.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    [HttpGet("zones")]
    public async Task<IActionResult> GetZones(
        [FromQuery] string? cameraId,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(cameraId))
        {
            var snapshot = await service.GetSnapshotAsync(cameraId, cancellationToken);
            return Ok(snapshot);
        }

        var latest = await service.GetLatestSnapshotAsync(cancellationToken);
        if (latest is null)
        {
            return Ok(new
            {
                camera_id = "",
                zones = new Dictionary<string, object>()
            });
        }

        return Ok(latest);
    }

    [HttpGet("zones/stream")]
    public async Task StreamZones(
        [FromServices] IZoneInventoryPublisher publisher,
        CancellationToken cancellationToken)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        var reader = publisher.Subscribe(cancellationToken);

        await foreach (var snapshot in reader.ReadAllAsync(cancellationToken))
        {
            var json = JsonSerializer.Serialize(snapshot);

            await Response.WriteAsync("event: zone_update\n", cancellationToken);
            await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    [HttpGet("spools")]
    public async Task<IActionResult> GetInventorySpools(
        [FromQuery] string? materialType,
        [FromQuery] string? colorName,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        var items = await service.GetInventorySpoolsAsync(materialType, colorName, cancellationToken);

        var result = items.Select(x => new
        {
            camera_id = x.CameraId,
            zone_name = x.ZoneName,
            spool_code = x.SpoolCode,
            material_type = x.MaterialType,
            color_name = x.ColorName,
            color_hex = x.ColorHex,
            last_seen_at_utc = x.LastSeenAtUtc,
            updated_at_utc = x.UpdatedAtUtc
        });

        return Ok(result);
    }
}