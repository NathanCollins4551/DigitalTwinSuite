namespace DigitalTwin.Domain.Entities;

public class PrinterFirmwareStatus
{
    public Guid Id { get; set; }

    public Guid PrinterId { get; set; }
    public Printer Printer { get; set; } = null!;

    public string? CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool? ForceUpdate { get; set; }
    public string? ReleaseStatus { get; set; }   // release/testing/etc.
    public string? Description { get; set; }
    public string? DownloadUrl { get; set; }

    public DateTimeOffset LastVersionSyncAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}