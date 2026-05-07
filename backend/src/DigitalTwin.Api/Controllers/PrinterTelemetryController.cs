using System.Text.Json;
using DigitalTwin.Application.Abstractions.Telemetry;
using DigitalTwin.Infrastructure.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DigitalTwin.Api.Controllers;

[ApiController]
[Route("api/printers/{deviceId}/telemetry")]
public class PrinterTelemetryController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetRecentTelemetry(
        string deviceId,
        [FromQuery] int minutes,
        [FromServices] IPrinterTelemetryWriter writer,
        CancellationToken cancellationToken)
    {
        var safeMinutes = minutes <= 0 ? 30 : minutes;
        var result = await writer.QueryRecentAsync(deviceId, safeMinutes, cancellationToken);
        return Ok(result);
    }

    [HttpGet("stream")]
    public async Task<IActionResult> StreamTelemetry(
        string deviceId,
        [FromServices] IPrinterTelemetryPublisher publisher,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        var canStream = await readService.CanStreamTelemetryAsync(deviceId, cancellationToken);
        if (!canStream)
        {
            return Conflict(new
            {
                message = "Telemetry stream is only available for printers that are RUNNING in simulation mode."
            });
        }

        Response.Headers.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var reader = publisher.Subscribe(deviceId, cancellationToken);

        try
        {
            await foreach (var point in reader.ReadAllAsync(cancellationToken))
            {
                var json = JsonSerializer.Serialize(point);

                await Response.WriteAsync($"event: telemetry\n", cancellationToken);
                await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // normal disconnect
        }

        return new EmptyResult();
    }

    [HttpGet("/api/printers/by-name/telemetry")]
    public async Task<IActionResult> GetRecentTelemetryByName(
        [FromQuery] string name,
        [FromQuery] int minutes,
        [FromServices] IPrinterTelemetryWriter writer,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            if (deviceId is null)
                return Ok(Array.Empty<object>());

            var safeMinutes = minutes <= 0 ? 30 : minutes;
            var result = await writer.QueryRecentAsync(deviceId, safeMinutes, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("/api/printers/by-name/telemetry/stream")]
    public async Task<IActionResult> StreamTelemetryByName(
        [FromQuery] string name,
        [FromServices] IPrinterTelemetryPublisher publisher,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            var canStream = deviceId is not null &&
                await readService.CanStreamTelemetryAsync(deviceId, cancellationToken);

            if (!canStream)
            {
                return Conflict(new
                {
                    message = "Telemetry stream is only available for printers that are RUNNING in simulation mode."
                });
            }

            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            var reader = publisher.Subscribe(deviceId!, cancellationToken);

            try
            {
                await foreach (var point in reader.ReadAllAsync(cancellationToken))
                {
                    var json = JsonSerializer.Serialize(point);

                    await Response.WriteAsync($"event: telemetry\n", cancellationToken);
                    await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // normal disconnect
            }

            return new EmptyResult();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
