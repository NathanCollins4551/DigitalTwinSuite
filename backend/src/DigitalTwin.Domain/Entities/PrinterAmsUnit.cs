namespace DigitalTwin.Domain.Entities;

public class PrinterAmsUnit
{
    public Guid Id { get; set; }

    public Guid PrinterId { get; set; }
    public Printer Printer { get; set; } = null!;

    public int AmsIndex { get; set; }                    // internal order in array
    public string? AmsDeviceId { get; set; }             // device_id from /version AMS block
    public string? CurrentVersion { get; set; }          // AMS current version
    public string? LatestVersion { get; set; }           // from firmware[0].version if present
    public bool? ForceUpdate { get; set; }
    public string? ReleaseStatus { get; set; }
    public string? Description { get; set; }
    public string? DownloadUrl { get; set; }

    public int? HumidityLevel { get; set; }              // from bind if available
    public decimal? TemperatureCelsius { get; set; }     // from bind if available

    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}