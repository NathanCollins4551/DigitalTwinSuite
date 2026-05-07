namespace DigitalTwin.Domain.Entities;

public class PrinterMessage
{
    public Guid Id { get; set; }

    public long ExternalMessageId { get; set; }           // messages[].id
    public Guid? PrinterId { get; set; }
    public Printer? Printer { get; set; }

    public long? ExternalTaskId { get; set; }             // messages[].taskMessage.id
    public Guid? RelatedPrinterTaskId { get; set; }       // resolved later if found
    public PrinterTask? RelatedPrinterTask { get; set; }

    public int? Type { get; set; }
    public int? IsRead { get; set; }

    public DateTimeOffset? CreateTimeUtc { get; set; }

    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }

    public int? TaskStatus { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? CoverUrl { get; set; }
    public int? DesignId { get; set; }
    public string? DesignTitle { get; set; }

    public string RawJson { get; set; } = null!;

    public DateTimeOffset SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}