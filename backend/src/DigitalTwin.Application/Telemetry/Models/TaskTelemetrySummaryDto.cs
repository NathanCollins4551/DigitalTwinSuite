namespace DigitalTwin.Application.Telemetry.Models;

public class TaskTelemetrySummaryDto
{
    public long ExternalTaskId { get; set; }
    public string TaskAlias { get; set; } = null!;
    public string DeviceId { get; set; } = null!;
    public string? DesignTitle { get; set; }
    public string StatusText { get; set; } = null!;

    public DateTimeOffset? StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }

    public bool HasTelemetry { get; set; }

    public decimal? LatestProgressPercent { get; set; }
    public int? LatestCurrentLayer { get; set; }
    public int? LatestTotalLayers { get; set; }

    public decimal? AvgNozzleTempC { get; set; }
    public decimal? AvgBedTempC { get; set; }
    public decimal? AvgChamberTempC { get; set; }

    public decimal? LatestFilamentRemainingGrams { get; set; }
    public decimal? LatestPowerWatts { get; set; }
    public decimal? LatestVibrationScore { get; set; }

    public string? Message { get; set; }
}
