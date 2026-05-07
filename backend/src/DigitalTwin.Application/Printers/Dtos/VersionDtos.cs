using System.Text.Json.Serialization;

namespace DigitalTwin.Application.Printers.Dtos;

public class VersionResponseDto
{
    [JsonPropertyName("devices")]
    public List<VersionPrinterDto> Devices { get; set; } = new();
}

public class VersionPrinterDto
{
    [JsonPropertyName("dev_id")]
    public string DeviceId { get; set; } = null!;

    [JsonPropertyName("version")]
    public string? CurrentVersion { get; set; }

    [JsonPropertyName("firmware")]
    public List<FirmwareEntryDto>? Firmware { get; set; }

    [JsonPropertyName("ams")]
    public List<VersionAmsDto>? Ams { get; set; }
}

public class VersionAmsDto
{
    [JsonPropertyName("device_id")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("version")]
    public string? CurrentVersion { get; set; }

    [JsonPropertyName("firmware")]
    public List<FirmwareEntryDto>? Firmware { get; set; }
}

public class FirmwareEntryDto
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("force_update")]
    public bool? ForceUpdate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}