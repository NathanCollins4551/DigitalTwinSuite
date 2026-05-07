namespace DigitalTwin.Domain.Entities;

public class PrinterSimulationControl
{
    public Guid Id { get; set; }

    public Guid PrinterId { get; set; }
    public Printer Printer { get; set; } = null!;

    public bool IsLocked { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }

    public string? SimulationState { get; set; }   // RUNNING, FAIL, SUCCESS
    public Guid? ActivePrinterTaskId { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}