namespace DigitalTwin.Application.Printers.Dtos;

public class PrinterListItemDto
{
    public string DeviceId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsOnline { get; set; }
    public string? PrintStatus { get; set; }
    public string? ModelName { get; set; }
    public string? ProductName { get; set; }
    public string? Structure { get; set; }
    public decimal? NozzleDiameterMm { get; set; }

    public string OperationalState { get; set; } = null!;
    public bool IsRunning { get; set; }
    public bool IsSimulationControlled { get; set; }

    public string? CurrentFirmwareVersion { get; set; }
    public string? LatestFirmwareVersion { get; set; }
    public bool? ForceUpdate { get; set; }

    public int AmsUnitCount { get; set; }

    public DateTimeOffset LastBindSyncAtUtc { get; set; }
    public DateTimeOffset? LastVersionSyncAtUtc { get; set; }
}

public class PrinterDetailDto
{
    public string DeviceId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsOnline { get; set; }
    public string? PrintStatus { get; set; }
    public string? ModelName { get; set; }
    public string? ProductName { get; set; }
    public string? Structure { get; set; }
    public decimal? NozzleDiameterMm { get; set; }

    public string OperationalState { get; set; } = null!;
    public bool IsRunning { get; set; }
    public bool IsSimulationControlled { get; set; }

    public PrinterFirmwareDto? Firmware { get; set; }
    public List<PrinterAmsUnitDto> AmsUnits { get; set; } = new();

    public PrinterTaskSummaryDto? LatestTask { get; set; }
    public PrinterMessageSummaryDto? LatestMessage { get; set; }
}

public class PrinterFirmwareDto
{
    public string? CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool? ForceUpdate { get; set; }
    public string? ReleaseStatus { get; set; }
    public string? Description { get; set; }
    public string? DownloadUrl { get; set; }
}

public class PrinterAmsUnitDto
{
    public int AmsIndex { get; set; }
    public string? AmsDeviceId { get; set; }
    public string? CurrentVersion { get; set; }
    public string? LatestVersion { get; set; }
    public bool? ForceUpdate { get; set; }
    public string? ReleaseStatus { get; set; }
    public int? HumidityLevel { get; set; }
    public decimal? TemperatureCelsius { get; set; }
}

public class PrinterTaskSummaryDto
{
    public long ExternalTaskId { get; set; }
    public string TaskAlias { get; set; } = null!;
    public string? DesignTitle { get; set; }
    public DateTimeOffset? StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }
    public int? FailedType { get; set; }
}

public class PrinterMessageSummaryDto
{
    public long ExternalMessageId { get; set; }
    public long? ExternalTaskId { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public DateTimeOffset? CreateTimeUtc { get; set; }
}

public class PrinterTaskItemDto
{
    public long ExternalTaskId { get; set; }
    public string TaskAlias { get; set; } = null!;
    public Guid? PrinterId { get; set; }
    public string DeviceId { get; set; } = null!;
    public string? DeviceName { get; set; }
    public string? DeviceModel { get; set; }
    public string? DesignTitle { get; set; }
    public string? DesignTitleTranslated { get; set; }
    public DateTimeOffset? StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }
    public int? CostTimeSeconds { get; set; }
    public int? LengthMm { get; set; }
    public decimal? WeightGrams { get; set; }
    public string? BedType { get; set; }
    public string? Mode { get; set; }
    public int? FailedType { get; set; }
    public string? CoverUrl { get; set; }

    public List<PrinterTaskAmsDetailItemDto> AmsDetails { get; set; } = new();
}

public class PrinterTaskAmsDetailItemDto
{
    public Guid Id { get; set; }
    public Guid PrinterTaskId { get; set; }
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
}

public class PrinterMessageItemDto
{
    public long ExternalMessageId { get; set; }
    public Guid? PrinterId { get; set; }
    public long? ExternalTaskId { get; set; }
    public Guid? RelatedPrinterTaskId { get; set; }
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
}

public class PrinterTimelineItemDto
{
    public string Source { get; set; } = null!;
    public string EventKind { get; set; } = null!;
    public DateTimeOffset TimestampUtc { get; set; }

    public long? ExternalTaskId { get; set; }
    public long? ExternalMessageId { get; set; }

    public string Title { get; set; } = null!;
    public string? Detail { get; set; }
}
