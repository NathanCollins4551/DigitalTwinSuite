using System.Text.Json.Serialization;

namespace DigitalTwin.Application.Printers.Dtos;

public class MessageResponseDto
{
    [JsonPropertyName("hits")]
    public List<MessageHitDto> Hits { get; set; } = new();
}

public class MessageHitDto
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("type")]
    public int? Type { get; set; }

    [JsonPropertyName("isread")]
    public int? IsRead { get; set; }

    [JsonPropertyName("createTime")]
    public DateTimeOffset? CreateTime { get; set; }

    [JsonPropertyName("taskMessage")]
    public MessageTaskDto? TaskMessage { get; set; }
}

public class MessageTaskDto
{
    [JsonPropertyName("id")]
    public long? Id { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("cover")]
    public string? Cover { get; set; }

    [JsonPropertyName("designId")]
    public int? DesignId { get; set; }

    [JsonPropertyName("designTitle")]
    public string? DesignTitle { get; set; }
}