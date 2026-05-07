using DigitalTwin.Application.Telemetry.Models;
using System.Threading.Channels;

namespace DigitalTwin.Application.Abstractions.Telemetry;

public interface IPrinterTelemetryPublisher
{
    ChannelReader<PrinterTelemetryPoint> Subscribe(string deviceId, CancellationToken cancellationToken = default);
    void Publish(PrinterTelemetryPoint point);
}