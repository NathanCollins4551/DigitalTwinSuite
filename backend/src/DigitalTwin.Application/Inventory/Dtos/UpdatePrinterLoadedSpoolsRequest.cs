namespace DigitalTwin.Application.Inventory.Dtos;

public class UpdatePrinterLoadedSpoolsRequest
{
    public List<UpdatePrinterLoadedSpoolItemDto> LoadedSpools { get; set; } = new();
}

public class UpdatePrinterLoadedSpoolItemDto
{
    public int SlotIndex { get; set; }

    public string SpoolCode { get; set; } = default!;
    public string MaterialType { get; set; } = default!;
    public string ColorName { get; set; } = default!;
    public string ColorHex { get; set; } = default!;

    public decimal? RemainingPercent { get; set; }
    public decimal? RemainingGrams { get; set; }

    public Guid? PrinterAmsUnitId { get; set; }
}