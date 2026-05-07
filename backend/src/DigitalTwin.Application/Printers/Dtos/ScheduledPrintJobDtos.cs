namespace DigitalTwin.Application.Printers.Dtos;

public class CreateScheduledPrintJobRequest
{
    public string FileName { get; set; } = null!;
    public int Priority { get; set; } = 5;

    public string? PreferredPrinterDeviceId { get; set; }
    public bool AllowAnyPrinter { get; set; }

    public string? RequestedMaterialType { get; set; }
    public string? RequestedColor { get; set; }

    public DateTimeOffset? RequestedStartAfterUtc { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }

    public string? Notes { get; set; }
}

public class ScheduledPrintJobDto
{
    public Guid Id { get; set; }

    public string FileName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int Priority { get; set; }

    public Guid? PreferredPrinterId { get; set; }
    public string? PreferredPrinterDeviceId { get; set; }
    public string? PreferredPrinterName { get; set; }

    public bool AllowAnyPrinter { get; set; }

    public string Status { get; set; } = null!;

    public Guid? AssignedPrinterId { get; set; }
    public string? AssignedPrinterDeviceId { get; set; }
    public string? AssignedPrinterName { get; set; }

    public Guid? StartedPrinterTaskId { get; set; }

    public string? RequestedMaterialType { get; set; }
    public string? RequestedColor { get; set; }

    public int? EstimatedDurationSeconds { get; set; }
    public decimal? EstimatedFilamentGrams { get; set; }

    public DateTimeOffset? RequestedStartAfterUtc { get; set; }
    public DateTimeOffset? DueAtUtc { get; set; }

    public string? Notes { get; set; }

    public bool IsSimulatedInput { get; set; }

    public string? SchedulerDecisionReason { get; set; }
    public string? CompatibilityNote { get; set; }
    public DateTimeOffset? EstimatedStartAtUtc { get; set; }
    public DateTimeOffset? EstimatedFinishAtUtc { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public class UpdateScheduledPrintJobPriorityRequest
{
    public int Priority { get; set; }
}

public class ScheduledPrintJobPreviewDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public int Priority { get; set; }
    public string Status { get; set; } = null!;
    public string? PreferredPrinterDeviceId { get; set; }
    public bool AllowAnyPrinter { get; set; }
    public int? EstimatedDurationSeconds { get; set; }

    public string? CompatibilityNote { get; set; }
    public DateTimeOffset? EstimatedStartAtUtc { get; set; }
    public DateTimeOffset? EstimatedFinishAtUtc { get; set; }
}

// public class SchedulerControlDto
// {
//     public bool IsPaused { get; set; }
//     public string? PauseReason { get; set; }
//     public DateTimeOffset UpdatedAtUtc { get; set; }
// }

// public class UpdateSchedulerPauseRequest
// {
//     public bool IsPaused { get; set; }
//     public string? PauseReason { get; set; }
// }