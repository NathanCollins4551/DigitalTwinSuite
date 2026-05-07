namespace DigitalTwin.Application.Telemetry.Models;

public class PrinterTelemetryPoint
{
    public string DeviceId { get; set; } = null!;
    public long ExternalTaskId { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }

    public string PrintStatus { get; set; } = null!;
    public bool IsSimulated { get; set; }

    public decimal ProgressPercent { get; set; }
    public int CurrentLayer { get; set; }
    public int TotalLayers { get; set; }

    public decimal NozzleTempC { get; set; }
    public decimal BedTempC { get; set; }
    public decimal ChamberTempC { get; set; }

    public int PrintSpeedPercent { get; set; }
    public decimal FilamentRemainingGrams { get; set; }
    public decimal PowerWatts { get; set; }
    public decimal VibrationScore { get; set; }

    public string? ErrorCode { get; set; }
}