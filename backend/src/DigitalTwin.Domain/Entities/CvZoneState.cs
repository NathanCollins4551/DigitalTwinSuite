namespace DigitalTwin.Domain.Entities;

public class CvZoneState
{
    public Guid Id { get; set; }

    public string CameraId { get; set; } = default!;
    public string ZoneName { get; set; } = default!;

    public string SpoolIdsJson { get; set; } = "[]";

    public int UnknownSpoolCount { get; set; }
    public int OtherObjectCount { get; set; }

    public DateTimeOffset? LastInventoryTsUtc { get; set; }
    public DateTimeOffset? LastAnomalyTsUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}