namespace DigitalTwin.Domain.Entities;

public class Printer
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = null!;   // dev_id
    public string Name { get; set; } = null!;       // dev_name
    public bool IsOnline { get; set; }              // dev_online
    public string? PrintStatus { get; set; }        // coarse state if available later

    public string? ModelName { get; set; }          // dev_model_name
    public string? ProductName { get; set; }        // dev_product_name
    public string? Structure { get; set; }          // optional if bind/print provides it
    public decimal? NozzleDiameterMm { get; set; }  // optional if available from print/details

    public bool IsAmsSupported { get; set; }

    public DateTimeOffset LastBindSyncAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public PrinterFirmwareStatus? FirmwareStatus { get; set; }
    public ICollection<PrinterAmsUnit> AmsUnits { get; set; } = new List<PrinterAmsUnit>();
    public PrinterSimulationControl? SimulationControl { get; set; }
}