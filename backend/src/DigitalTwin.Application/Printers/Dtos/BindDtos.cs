using System.Text.Json.Serialization;

namespace DigitalTwin.Application.Printers.Dtos;

public class BindResponseDto
{
    [JsonPropertyName("devices")]
    public List<BindPrinterDto> Devices { get; set; } = new();
}

public class BindPrinterDto
{
    [JsonPropertyName("dev_id")]
    public string DeviceId { get; set; } = null!;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("online")]
    public bool IsOnline { get; set; }

    [JsonPropertyName("print_status")]
    public string? PrintStatus { get; set; }

    [JsonPropertyName("dev_model_name")]
    public string? ModelName { get; set; }

    [JsonPropertyName("dev_product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("dev_structure")]
    public string? Structure { get; set; }

    [JsonPropertyName("nozzle_diameter")]
    public decimal? NozzleDiameter { get; set; }

    [JsonPropertyName("ams")]
    public List<BindAmsDto>? Ams { get; set; }
}

public class BindAmsDto
{
    [JsonPropertyName("humidity")]
    public string? Humidity { get; set; }

    [JsonPropertyName("temp")]
    public string? Temperature { get; set; }
}