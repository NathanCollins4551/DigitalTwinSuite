using DigitalTwin.Infrastructure.Queries;
using DigitalTwin.Infrastructure.Inventory;
using DigitalTwin.Application.Inventory.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace DigitalTwin.Api.Controllers;

[ApiController]
[Route("api/printers")]
public class PrintersController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetPrinters(
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetPrintersAsync(cancellationToken));
    }

    [HttpGet("running")]
    public async Task<IActionResult> GetRunningPrinters(
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetRunningPrintersAsync(cancellationToken));
    }

    // Routes for Querying with deviceId

    [HttpGet("{deviceId}")]
    public async Task<IActionResult> GetPrinterDetail(
        string deviceId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        var result = await readService.GetPrinterDetailAsync(deviceId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{deviceId}/firmware")]
    public async Task<IActionResult> GetPrinterFirmware(
        string deviceId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        var result = await readService.GetPrinterFirmwareAsync(deviceId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{deviceId}/ams-units")]
    public async Task<IActionResult> GetPrinterAmsUnits(
        string deviceId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetPrinterAmsUnitsAsync(deviceId, cancellationToken));
    }

    [HttpGet("{deviceId}/tasks")]
    public async Task<IActionResult> GetPrinterTasks(
        string deviceId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetPrinterTasksAsync(deviceId, cancellationToken));
    }

    [HttpGet("{deviceId}/messages")]
    public async Task<IActionResult> GetPrinterMessages(
        string deviceId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetPrinterMessagesAsync(deviceId, cancellationToken));
    }

    [HttpGet("{deviceId}/timeline")]
    public async Task<IActionResult> GetPrinterTimeline(
        string deviceId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetPrinterTimelineAsync(deviceId, cancellationToken));
    }

    [HttpGet("{deviceId}/loaded-spools")]
    public async Task<IActionResult> GetLoadedSpools(
        string deviceId,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetPrinterLoadedSpoolsAsync(deviceId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{deviceId}/replacement-suggestions")]
    public async Task<IActionResult> GetReplacementSuggestions(
        string deviceId,
        [FromQuery] decimal lowThresholdPercent,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        var threshold = lowThresholdPercent <= 0 ? 15m : lowThresholdPercent;

        var result = await service.GetReplacementSuggestionsAsync(deviceId, threshold, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{deviceId}/loaded-spools")]
    public async Task<IActionResult> UpdateLoadedSpools(
        string deviceId,
        [FromBody] UpdatePrinterLoadedSpoolsRequest request,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.UpdatePrinterLoadedSpoolsAsync(deviceId, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    // Routes for Querying with deviceName
    [HttpGet("by-name")]
    public async Task<IActionResult> GetPrinterDetailByName(
        [FromQuery] string name,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new
            {
                message = "Query parameter 'name' is required."
            });
        }

        try
        {
            var result = await readService.GetPrinterDetailByNameAsync(name, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

    [HttpGet("by-name/firmware")]
    public async Task<IActionResult> GetPrinterFirmwareByName(
        [FromQuery] string name,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            if (deviceId is null)
                return NotFound();

            var result = await readService.GetPrinterFirmwareAsync(deviceId, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("by-name/ams-units")]
    public async Task<IActionResult> GetPrinterAmsUnitsByName(
        [FromQuery] string name,
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

            return Ok(await readService.GetPrinterAmsUnitsAsync(deviceId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("by-name/tasks")]
    public async Task<IActionResult> GetPrinterTasksByName(
        [FromQuery] string name,
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

            return Ok(await readService.GetPrinterTasksAsync(deviceId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("by-name/messages")]
    public async Task<IActionResult> GetPrinterMessagesByName(
        [FromQuery] string name,
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

            return Ok(await readService.GetPrinterMessagesAsync(deviceId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("by-name/timeline")]
    public async Task<IActionResult> GetPrinterTimelineByName(
        [FromQuery] string name,
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

            return Ok(await readService.GetPrinterTimelineAsync(deviceId, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("by-name/loaded-spools")]
    public async Task<IActionResult> GetLoadedSpoolsByName(
        [FromQuery] string name,
        [FromServices] PrinterReadService readService,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            if (deviceId is null)
                return Ok(Array.Empty<PrinterLoadedSpoolDto>());

            var result = await service.GetPrinterLoadedSpoolsAsync(deviceId, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("by-name/replacement-suggestions")]
    public async Task<IActionResult> GetReplacementSuggestionsByName(
        [FromQuery] string name,
        [FromQuery] decimal lowThresholdPercent,
        [FromServices] PrinterReadService readService,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            if (deviceId is null)
                return NotFound();

            var threshold = lowThresholdPercent <= 0 ? 15m : lowThresholdPercent;
            var result = await service.GetReplacementSuggestionsAsync(deviceId, threshold, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("by-name/loaded-spools")]
    public async Task<IActionResult> UpdateLoadedSpoolsByName(
        [FromQuery] string name,
        [FromBody] UpdatePrinterLoadedSpoolsRequest request,
        [FromServices] PrinterReadService readService,
        [FromServices] CvZoneStateService service,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            if (deviceId is null)
                return NotFound();

            var result = await service.UpdatePrinterLoadedSpoolsAsync(deviceId, request, cancellationToken);
            return result is null ? NotFound() : Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new
            {
                message = ex.Message
            });
        }
    }

}
