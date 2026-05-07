using System.Text.Json.Serialization;

namespace DigitalTwin.Application.Printers.Dtos;

public class TaskResponseDto
{
    [JsonPropertyName("hits")]
    public List<TaskHitDto> Hits { get; set; } = new();
}

public class TaskHitDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = null!;

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("deviceModel")]
    public string? DeviceModel { get; set; }

    [JsonPropertyName("designTitle")]
    public string? DesignTitle { get; set; }

    [JsonPropertyName("designTitleTranslated")]
    public string? DesignTitleTranslated { get; set; }

    [JsonPropertyName("startTime")]
    public DateTimeOffset? StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTimeOffset? EndTime { get; set; }

    [JsonPropertyName("costTime")]
    public int? CostTime { get; set; }

    [JsonPropertyName("length")]
    public int? Length { get; set; }

    [JsonPropertyName("bedType")]
    public string? BedType { get; set; }

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("failedType")]
    public int? FailedType { get; set; }

    [JsonPropertyName("cover")]
    public string? Cover { get; set; }

    [JsonPropertyName("amsDetailMapping")]
    public List<TaskAmsDetailDto>? AmsDetailMapping { get; set; }
}

public class TaskAmsDetailDto
{
    [JsonPropertyName("ams")]
    public int? Ams { get; set; }

    [JsonPropertyName("amsId")]
    public int? AmsId { get; set; }

    [JsonPropertyName("slotId")]
    public int? SlotId { get; set; }

    [JsonPropertyName("nozzleId")]
    public int? NozzleId { get; set; }

    [JsonPropertyName("filamentId")]
    public string? FilamentId { get; set; }

    [JsonPropertyName("filamentType")]
    public string? FilamentType { get; set; }

    [JsonPropertyName("targetFilamentType")]
    public string? TargetFilamentType { get; set; }

    [JsonPropertyName("sourceColor")]
    public string? SourceColor { get; set; }

    [JsonPropertyName("targetColor")]
    public string? TargetColor { get; set; }

    [JsonPropertyName("weight")]
    public decimal? Weight { get; set; }
}