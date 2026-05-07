using System.Text.Json.Serialization;

namespace DigitalTwin.Application.Inventory.Dtos;

public class CvZoneInventoryMessageDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("ts")]
    public DateTimeOffset Ts { get; set; }

    [JsonPropertyName("camera_id")]
    public string CameraId { get; set; } = default!;

    [JsonPropertyName("zones")]
    public Dictionary<string, CvZoneInventoryZoneDto> Zones { get; set; } = new();
}

public class CvZoneInventoryZoneDto
{
    [JsonPropertyName("spool_ids")]
    public List<string> SpoolIds { get; set; } = new();

    [JsonPropertyName("unknown_spool_count")]
    public int UnknownSpoolCount { get; set; }
}

public class CvZoneAnomalyMessageDto
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("ts")]
    public DateTimeOffset Ts { get; set; }

    [JsonPropertyName("camera_id")]
    public string CameraId { get; set; } = default!;

    [JsonPropertyName("zones")]
    public Dictionary<string, CvZoneAnomalyZoneDto> Zones { get; set; } = new();
}

public class CvZoneAnomalyZoneDto
{
    [JsonPropertyName("other_count")]
    public int OtherCount { get; set; }
}

public class ZoneSnapshotResponseDto
{
    [JsonPropertyName("camera_id")]
    public string CameraId { get; set; } = default!;

    [JsonPropertyName("zones")]
    public Dictionary<string, ZoneViewDto> Zones { get; set; } = new();
}

public class ZoneViewDto
{
    [JsonPropertyName("colors")]
    public Dictionary<string, int> Colors { get; set; } = new();

    [JsonPropertyName("unknown_spool_count")]
    public int UnknownSpoolCount { get; set; }

    [JsonPropertyName("other_object_count")]
    public int OtherObjectCount { get; set; }

    [JsonPropertyName("updated_at_utc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }
}