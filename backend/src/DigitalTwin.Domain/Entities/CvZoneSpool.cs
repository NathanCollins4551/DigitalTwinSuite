namespace DigitalTwin.Domain.Entities;

public class CvZoneSpool
{
    public Guid Id { get; set; }

    public string CameraId { get; set; } = default!;
    public string ZoneName { get; set; } = default!;

    public string SpoolCode { get; set; } = default!;
    public string MaterialType { get; set; } = default!;
    public string ColorName { get; set; } = default!;
    public string ColorHex { get; set; } = default!;

    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}