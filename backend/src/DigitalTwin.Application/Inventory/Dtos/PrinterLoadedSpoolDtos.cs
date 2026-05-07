namespace DigitalTwin.Application.Inventory.Dtos;

public class PrinterLoadedSpoolDto
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = default!;
    public string PrinterName { get; set; } = default!;

    public int SlotIndex { get; set; }

    public string SpoolCode { get; set; } = default!;
    public string MaterialType { get; set; } = default!;
    public string ColorName { get; set; } = default!;
    public string ColorHex { get; set; } = default!;

    public decimal? RemainingPercent { get; set; }
    public decimal? RemainingGrams { get; set; }

    public bool IsActive { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public class PrinterReplacementSuggestionDto
{
    public string DeviceId { get; set; } = default!;
    public List<PrinterReplacementItemDto> Replacements { get; set; } = new();
}

public class PrinterReplacementItemDto
{
    public int SlotIndex { get; set; }

    public string CurrentSpoolCode { get; set; } = default!;
    public string MaterialType { get; set; } = default!;
    public string ColorName { get; set; } = default!;
    public string ColorHex { get; set; } = default!;

    public decimal? RemainingPercent { get; set; }
    public decimal? RemainingGrams { get; set; }

    public List<InventoryZoneMatchDto> SuggestedZones { get; set; } = new();
}

public class InventoryZoneMatchDto
{
    public string ZoneName { get; set; } = default!;
    public List<string> MatchingSpoolCodes { get; set; } = new();
}