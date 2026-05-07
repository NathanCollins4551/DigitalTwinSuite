namespace DigitalTwin.Domain.Entities;

public class PrinterLoadedSpool
{
    public Guid Id { get; set; }

    public Guid PrinterId { get; set; }
    public Printer Printer { get; set; } = default!;

    public Guid? PrinterAmsUnitId { get; set; }
    public PrinterAmsUnit? PrinterAmsUnit { get; set; }

    public int SlotIndex { get; set; }

    public string SpoolCode { get; set; } = default!;
    public string MaterialType { get; set; } = default!;
    public string ColorName { get; set; } = default!;
    public string ColorHex { get; set; } = default!;

    public decimal? RemainingPercent { get; set; }
    public decimal? RemainingGrams { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}