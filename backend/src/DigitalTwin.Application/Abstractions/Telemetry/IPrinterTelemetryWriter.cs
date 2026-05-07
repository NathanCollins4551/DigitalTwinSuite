using DigitalTwin.Application.Telemetry.Models;

namespace DigitalTwin.Application.Abstractions.Telemetry;

public interface IPrinterTelemetryWriter
{
    Task WriteAsync(PrinterTelemetryPoint point, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrinterTelemetryPoint>> QueryRecentAsync(
        string deviceId,
        int minutes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PrinterTelemetryPoint>> QueryTaskRangeAsync(
        string deviceId,
        long externalTaskId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken = default);
}