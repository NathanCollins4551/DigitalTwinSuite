namespace DigitalTwin.Domain.Entities;

public class PrinterTaskAmsDetail
{
    public Guid Id { get; set; }

    public Guid PrinterTaskId { get; set; }
    public PrinterTask PrinterTask { get; set; } = null!;

    public int? Ams { get; set; }
    public int? AmsId { get; set; }
    public int? SlotId { get; set; }
    public int? NozzleId { get; set; }

    public string? FilamentId { get; set; }
    public string? FilamentType { get; set; }
    public string? TargetFilamentType { get; set; }

    public string? SourceColor { get; set; }
    public string? TargetColor { get; set; }

    public decimal? WeightGrams { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}