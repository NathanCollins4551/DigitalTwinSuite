namespace DigitalTwin.Domain.Entities;

public class ScheduledPrintJob
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int Priority { get; set; }

    public Guid? PreferredPrinterId { get; set; }
    public Printer? PreferredPrinter { get; set; }

    public bool AllowAnyPrinter { get; set; }

    public string Status { get; set; } = "QUEUED"; // QUEUED, ASSIGNED, RUNNING, COMPLETED, FAILED, CANCELLED

    public Guid? AssignedPrinterId { get; set; }
    public Printer? AssignedPrinter { get; set; }

    public Guid? StartedPrinterTaskId { get; set; }
    public PrinterTask? StartedPrinterTask { get; set; }

    public string? RequestedMaterialType { get; set; }
    public string? RequestedColor { get; set; }

    public int? EstimatedDurationSeconds { get; set; }
    public decimal? EstimatedFilamentGrams { get; set; }

    public DateTimeOffset? RequestedStartAfterUtc { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }

    public string? Notes { get; set; }

    public bool IsSimulatedInput { get; set; } = true;

    public string? SchedulerDecisionReason { get; set; }
    public string? CompatibilityNote { get; set; }
    public DateTimeOffset? EstimatedStartAtUtc { get; set; }
    public DateTimeOffset? EstimatedFinishAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}