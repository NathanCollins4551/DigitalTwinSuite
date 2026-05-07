using DigitalTwin.Infrastructure.Simulation;
using DigitalTwin.Infrastructure.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DigitalTwin.Api.Controllers;

[ApiController]
[Route("api/printers/{deviceId}/simulate")]
public class PrinterSimulationController : ControllerBase
{
    [HttpPost("start")]
    public async Task<IActionResult> StartPrinter(
        string deviceId,
        [FromBody] StartPrinterSimulationRequest request,
        [FromServices] PrinterSimulationService simulationService,
        CancellationToken cancellationToken)
    {
        var result = await simulationService.StartPrinterAsync(
            deviceId,
            request.DesignTitle,
            request.SimulatedDurationSeconds,
            cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopPrinter(
        string deviceId,
        [FromServices] PrinterSimulationService simulationService,
        CancellationToken cancellationToken)
    {
        var result = await simulationService.StopPrinterAsync(deviceId, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    [HttpPost("/api/printers/by-name/simulate/start")]
    public async Task<IActionResult> StartPrinterByName(
        [FromQuery] string name,
        [FromBody] StartPrinterSimulationRequest request,
        [FromServices] PrinterReadService readService,
        [FromServices] PrinterSimulationService simulationService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            if (deviceId is null)
                return BadRequest(new { message = "Printer not found." });

            var result = await simulationService.StartPrinterAsync(
                deviceId,
                request.DesignTitle,
                request.SimulatedDurationSeconds,
                cancellationToken);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("/api/printers/by-name/simulate/stop")]
    public async Task<IActionResult> StopPrinterByName(
        [FromQuery] string name,
        [FromServices] PrinterReadService readService,
        [FromServices] PrinterSimulationService simulationService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "Query parameter 'name' is required." });

        try
        {
            var deviceId = await readService.ResolveDeviceIdByNameAsync(name, cancellationToken);
            if (deviceId is null)
                return BadRequest(new { message = "Printer not found." });

            var result = await simulationService.StopPrinterAsync(deviceId, cancellationToken);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

public class StartPrinterSimulationRequest
{
    public string? DesignTitle { get; set; }
    public int SimulatedDurationSeconds { get; set; } = 300;
}
