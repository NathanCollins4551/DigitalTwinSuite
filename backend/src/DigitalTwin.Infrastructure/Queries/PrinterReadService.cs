using DigitalTwin.Application.Printers.Dtos;
using DigitalTwin.Infrastructure.Persistence;
using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Queries;

public class PrinterReadService
{
    private readonly DigitalTwinDbContext _db;

    public PrinterReadService(DigitalTwinDbContext db)
    {
        _db = db;
    }

    public async Task<List<PrinterListItemDto>> GetPrintersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var printers = await _db.Printers
            .AsNoTracking()
            .Include(p => p.FirmwareStatus)
            .Include(p => p.AmsUnits)
            .Include(p => p.SimulationControl)
            .ToListAsync(cancellationToken);

        return printers
            .Select(p => MapPrinterListItem(p, now))
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task<List<PrinterListItemDto>> GetRunningPrintersAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var printers = await _db.Printers
            .AsNoTracking()
            .Include(p => p.FirmwareStatus)
            .Include(p => p.AmsUnits)
            .Include(p => p.SimulationControl)
            .ToListAsync(cancellationToken);

        return printers
            .Select(p => MapPrinterListItem(p, now))
            .Where(x => x.IsRunning)
            .OrderBy(x => x.Name)
            .ToList();
    }

    public async Task<PrinterDetailDto?> GetPrinterDetailAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var printer = await CreatePrinterDetailQuery()
            .FirstOrDefaultAsync(p => p.DeviceId == deviceId, cancellationToken);

        return printer is null
            ? null
            : await BuildPrinterDetailAsync(printer, cancellationToken);
    }

    public async Task<string?> ResolveDeviceIdByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var normalizedName = name.Trim();

        var deviceIds = await _db.Printers
            .AsNoTracking()
            .Where(p => p.Name == normalizedName)
            .Select(p => p.DeviceId)
            .Take(2)
            .ToListAsync(cancellationToken);

        return deviceIds.Count switch
        {
            0 => null,
            > 1 => throw new InvalidOperationException($"Multiple printers found with name '{normalizedName}'."),
            _ => deviceIds[0]
        };
    }

    public async Task<PrinterDetailDto?> GetPrinterDetailByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var deviceId = await ResolveDeviceIdByNameAsync(name, cancellationToken);
        return deviceId is null
            ? null
            : await GetPrinterDetailAsync(deviceId, cancellationToken);
    }

    public async Task<long?> ResolveExternalTaskIdByTaskAliasAsync(string taskAlias, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taskAlias))
            return null;

        var normalizedTaskAlias = taskAlias.Trim().ToUpperInvariant();

        return await _db.PrinterTasks
            .AsNoTracking()
            .Where(x => x.TaskAlias == normalizedTaskAlias)
            .Select(x => (long?)x.ExternalTaskId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Printer> CreatePrinterDetailQuery()
    {
        return _db.Printers
            .AsNoTracking()
            .Include(p => p.FirmwareStatus)
            .Include(p => p.AmsUnits)
            .Include(p => p.SimulationControl);
    }

    private async Task<PrinterDetailDto> BuildPrinterDetailAsync(Printer printer, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var operationalState = ResolveOperationalState(printer, now);
        var isSimulationControlled = IsSimulationLocked(printer, now);

        var latestTask = await _db.PrinterTasks
            .AsNoTracking()
            .Where(x => x.DeviceId == printer.DeviceId)
            .OrderByDescending(x => x.StartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        var latestMessage = await _db.PrinterMessages
            .AsNoTracking()
            .Where(x => x.DeviceId == printer.DeviceId)
            .OrderByDescending(x => x.CreateTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new PrinterDetailDto
        {
            DeviceId = printer.DeviceId,
            Name = printer.Name,
            IsOnline = printer.IsOnline,
            PrintStatus = printer.PrintStatus,
            ModelName = printer.ModelName,
            ProductName = printer.ProductName,
            Structure = printer.Structure,
            NozzleDiameterMm = printer.NozzleDiameterMm,

            OperationalState = operationalState,
            IsRunning = string.Equals(operationalState, "RUNNING", StringComparison.OrdinalIgnoreCase),
            IsSimulationControlled = isSimulationControlled,

            Firmware = printer.FirmwareStatus == null ? null : new PrinterFirmwareDto
            {
                CurrentVersion = printer.FirmwareStatus.CurrentVersion,
                LatestVersion = printer.FirmwareStatus.LatestVersion,
                ForceUpdate = printer.FirmwareStatus.ForceUpdate,
                ReleaseStatus = printer.FirmwareStatus.ReleaseStatus,
                Description = printer.FirmwareStatus.Description,
                DownloadUrl = printer.FirmwareStatus.DownloadUrl
            },
            AmsUnits = printer.AmsUnits
                .OrderBy(x => x.AmsIndex)
                .Select(x => new PrinterAmsUnitDto
                {
                    AmsIndex = x.AmsIndex,
                    AmsDeviceId = x.AmsDeviceId,
                    CurrentVersion = x.CurrentVersion,
                    LatestVersion = x.LatestVersion,
                    ForceUpdate = x.ForceUpdate,
                    ReleaseStatus = x.ReleaseStatus,
                    HumidityLevel = x.HumidityLevel,
                    TemperatureCelsius = x.TemperatureCelsius
                })
                .ToList(),
            LatestTask = latestTask == null ? null : new PrinterTaskSummaryDto
            {
                ExternalTaskId = latestTask.ExternalTaskId,
                TaskAlias = latestTask.TaskAlias,
                DesignTitle = latestTask.DesignTitle,
                StartTimeUtc = latestTask.StartTimeUtc,
                EndTimeUtc = latestTask.EndTimeUtc,
                FailedType = latestTask.FailedType
            },
            LatestMessage = latestMessage == null ? null : new PrinterMessageSummaryDto
            {
                ExternalMessageId = latestMessage.ExternalMessageId,
                ExternalTaskId = latestMessage.ExternalTaskId,
                Title = latestMessage.Title,
                Detail = latestMessage.Detail,
                CreateTimeUtc = latestMessage.CreateTimeUtc
            }
        };
    }

    public async Task<PrinterFirmwareDto?> GetPrinterFirmwareAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var printer = await _db.Printers
            .AsNoTracking()
            .Include(x => x.FirmwareStatus)
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (printer?.FirmwareStatus == null)
            return null;

        return new PrinterFirmwareDto
        {
            CurrentVersion = printer.FirmwareStatus.CurrentVersion,
            LatestVersion = printer.FirmwareStatus.LatestVersion,
            ForceUpdate = printer.FirmwareStatus.ForceUpdate,
            ReleaseStatus = printer.FirmwareStatus.ReleaseStatus,
            Description = printer.FirmwareStatus.Description,
            DownloadUrl = printer.FirmwareStatus.DownloadUrl
        };
    }

    public async Task<List<PrinterAmsUnitDto>> GetPrinterAmsUnitsAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _db.Printers
            .AsNoTracking()
            .Where(p => p.DeviceId == deviceId)
            .SelectMany(p => p.AmsUnits)
            .OrderBy(x => x.AmsIndex)
            .Select(x => new PrinterAmsUnitDto
            {
                AmsIndex = x.AmsIndex,
                AmsDeviceId = x.AmsDeviceId,
                CurrentVersion = x.CurrentVersion,
                LatestVersion = x.LatestVersion,
                ForceUpdate = x.ForceUpdate,
                ReleaseStatus = x.ReleaseStatus,
                HumidityLevel = x.HumidityLevel,
                TemperatureCelsius = x.TemperatureCelsius
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PrinterTaskItemDto>> GetPrinterTasksAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _db.PrinterTasks
            .AsNoTracking()
            .Include(x => x.AmsDetails)
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.StartTimeUtc)
            .Select(x => new PrinterTaskItemDto
            {
                ExternalTaskId = x.ExternalTaskId,
                TaskAlias = x.TaskAlias,
                PrinterId = x.PrinterId,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                DeviceModel = x.DeviceModel,
                DesignTitle = x.DesignTitle,
                DesignTitleTranslated = x.DesignTitleTranslated,
                StartTimeUtc = x.StartTimeUtc,
                EndTimeUtc = x.EndTimeUtc,
                CostTimeSeconds = x.CostTimeSeconds,
                LengthMm = x.LengthMm,
                WeightGrams = x.WeightGrams,
                BedType = x.BedType,
                Mode = x.Mode,
                FailedType = x.FailedType,
                CoverUrl = x.CoverUrl,
                AmsDetails = x.AmsDetails.Select(a => new PrinterTaskAmsDetailItemDto
                {
                    Id = a.Id,
                    PrinterTaskId = a.PrinterTaskId,
                    Ams = a.Ams,
                    AmsId = a.AmsId,
                    SlotId = a.SlotId,
                    NozzleId = a.NozzleId,
                    FilamentId = a.FilamentId,
                    FilamentType = a.FilamentType,
                    TargetFilamentType = a.TargetFilamentType,
                    SourceColor = a.SourceColor,
                    TargetColor = a.TargetColor,
                    WeightGrams = a.WeightGrams
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PrinterTaskItemDto?> GetTaskByExternalTaskIdAsync(long externalTaskId, CancellationToken cancellationToken = default)
    {
        return await _db.PrinterTasks
            .AsNoTracking()
            .Include(x => x.AmsDetails)
            .Where(x => x.ExternalTaskId == externalTaskId)
            .Select(x => new PrinterTaskItemDto
            {
                ExternalTaskId = x.ExternalTaskId,
                TaskAlias = x.TaskAlias,
                PrinterId = x.PrinterId,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                DeviceModel = x.DeviceModel,
                DesignTitle = x.DesignTitle,
                DesignTitleTranslated = x.DesignTitleTranslated,
                StartTimeUtc = x.StartTimeUtc,
                EndTimeUtc = x.EndTimeUtc,
                CostTimeSeconds = x.CostTimeSeconds,
                LengthMm = x.LengthMm,
                WeightGrams = x.WeightGrams,
                BedType = x.BedType,
                Mode = x.Mode,
                FailedType = x.FailedType,
                CoverUrl = x.CoverUrl,
                AmsDetails = x.AmsDetails.Select(a => new PrinterTaskAmsDetailItemDto
                {
                    Id = a.Id,
                    PrinterTaskId = a.PrinterTaskId,
                    Ams = a.Ams,
                    AmsId = a.AmsId,
                    SlotId = a.SlotId,
                    NozzleId = a.NozzleId,
                    FilamentId = a.FilamentId,
                    FilamentType = a.FilamentType,
                    TargetFilamentType = a.TargetFilamentType,
                    SourceColor = a.SourceColor,
                    TargetColor = a.TargetColor,
                    WeightGrams = a.WeightGrams
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PrinterTaskItemDto>> GetAllTasksAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PrinterTasks
            .AsNoTracking()
            .Include(x => x.AmsDetails)
            .OrderByDescending(x => x.StartTimeUtc)
            .Select(x => new PrinterTaskItemDto
            {
                ExternalTaskId = x.ExternalTaskId,
                TaskAlias = x.TaskAlias,
                PrinterId = x.PrinterId,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                DeviceModel = x.DeviceModel,
                DesignTitle = x.DesignTitle,
                DesignTitleTranslated = x.DesignTitleTranslated,
                StartTimeUtc = x.StartTimeUtc,
                EndTimeUtc = x.EndTimeUtc,
                CostTimeSeconds = x.CostTimeSeconds,
                LengthMm = x.LengthMm,
                WeightGrams = x.WeightGrams,
                BedType = x.BedType,
                Mode = x.Mode,
                FailedType = x.FailedType,
                CoverUrl = x.CoverUrl,
                AmsDetails = x.AmsDetails.Select(a => new PrinterTaskAmsDetailItemDto
                {
                    Id = a.Id,
                    PrinterTaskId = a.PrinterTaskId,
                    Ams = a.Ams,
                    AmsId = a.AmsId,
                    SlotId = a.SlotId,
                    NozzleId = a.NozzleId,
                    FilamentId = a.FilamentId,
                    FilamentType = a.FilamentType,
                    TargetFilamentType = a.TargetFilamentType,
                    SourceColor = a.SourceColor,
                    TargetColor = a.TargetColor,
                    WeightGrams = a.WeightGrams
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PrinterMessageItemDto>> GetPrinterMessagesAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        return await _db.PrinterMessages
            .AsNoTracking()
            .Where(x => x.DeviceId == deviceId)
            .OrderByDescending(x => x.CreateTimeUtc)
            .Select(x => new PrinterMessageItemDto
            {
                ExternalMessageId = x.ExternalMessageId,
                PrinterId = x.PrinterId,
                ExternalTaskId = x.ExternalTaskId,
                RelatedPrinterTaskId = x.RelatedPrinterTaskId,
                Type = x.Type,
                IsRead = x.IsRead,
                CreateTimeUtc = x.CreateTimeUtc,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                TaskStatus = x.TaskStatus,
                Title = x.Title,
                Detail = x.Detail,
                CoverUrl = x.CoverUrl,
                DesignId = x.DesignId,
                DesignTitle = x.DesignTitle
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PrinterMessageItemDto?> GetMessageByExternalMessageIdAsync(long externalMessageId, CancellationToken cancellationToken = default)
    {
        return await _db.PrinterMessages
            .AsNoTracking()
            .Where(x => x.ExternalMessageId == externalMessageId)
            .Select(x => new PrinterMessageItemDto
            {
                ExternalMessageId = x.ExternalMessageId,
                PrinterId = x.PrinterId,
                ExternalTaskId = x.ExternalTaskId,
                RelatedPrinterTaskId = x.RelatedPrinterTaskId,
                Type = x.Type,
                IsRead = x.IsRead,
                CreateTimeUtc = x.CreateTimeUtc,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                TaskStatus = x.TaskStatus,
                Title = x.Title,
                Detail = x.Detail,
                CoverUrl = x.CoverUrl,
                DesignId = x.DesignId,
                DesignTitle = x.DesignTitle
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<PrinterMessageItemDto>> GetAllMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PrinterMessages
            .AsNoTracking()
            .OrderByDescending(x => x.CreateTimeUtc)
            .Select(x => new PrinterMessageItemDto
            {
                ExternalMessageId = x.ExternalMessageId,
                PrinterId = x.PrinterId,
                ExternalTaskId = x.ExternalTaskId,
                RelatedPrinterTaskId = x.RelatedPrinterTaskId,
                Type = x.Type,
                IsRead = x.IsRead,
                CreateTimeUtc = x.CreateTimeUtc,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                TaskStatus = x.TaskStatus,
                Title = x.Title,
                Detail = x.Detail,
                CoverUrl = x.CoverUrl,
                DesignId = x.DesignId,
                DesignTitle = x.DesignTitle
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PrinterTaskAmsDetailItemDto>> GetAllTaskAmsDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.PrinterTaskAmsDetails
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PrinterTaskAmsDetailItemDto
            {
                Id = x.Id,
                PrinterTaskId = x.PrinterTaskId,
                Ams = x.Ams,
                AmsId = x.AmsId,
                SlotId = x.SlotId,
                NozzleId = x.NozzleId,
                FilamentId = x.FilamentId,
                FilamentType = x.FilamentType,
                TargetFilamentType = x.TargetFilamentType,
                SourceColor = x.SourceColor,
                TargetColor = x.TargetColor,
                WeightGrams = x.WeightGrams
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PrinterTaskAmsDetailItemDto>> GetTaskAmsDetailsByExternalTaskIdAsync(long externalTaskId, CancellationToken cancellationToken = default)
    {
        return await _db.PrinterTaskAmsDetails
            .AsNoTracking()
            .Where(x => x.PrinterTask.ExternalTaskId == externalTaskId)
            .OrderBy(x => x.Ams)
            .ThenBy(x => x.AmsId)
            .ThenBy(x => x.SlotId)
            .Select(x => new PrinterTaskAmsDetailItemDto
            {
                Id = x.Id,
                PrinterTaskId = x.PrinterTaskId,
                Ams = x.Ams,
                AmsId = x.AmsId,
                SlotId = x.SlotId,
                NozzleId = x.NozzleId,
                FilamentId = x.FilamentId,
                FilamentType = x.FilamentType,
                TargetFilamentType = x.TargetFilamentType,
                SourceColor = x.SourceColor,
                TargetColor = x.TargetColor,
                WeightGrams = x.WeightGrams
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PrinterTimelineItemDto>> GetPrinterTimelineAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var tasks = await _db.PrinterTasks
            .AsNoTracking()
            .Where(x => x.DeviceId == deviceId)
            .Select(x => new PrinterTimelineItemDto
            {
                Source = "task",
                EventKind = x.EndTimeUtc != null ? "task_finished" : "task_started",
                TimestampUtc = x.EndTimeUtc ?? x.StartTimeUtc ?? x.CreatedAtUtc,
                ExternalTaskId = x.ExternalTaskId,
                ExternalMessageId = null,
                Title = x.DesignTitle ?? $"Task {x.ExternalTaskId}",
                Detail = x.EndTimeUtc != null
                    ? $"Task finished. FailedType={x.FailedType}"
                    : "Task started"
            })
            .ToListAsync(cancellationToken);

        var messages = await _db.PrinterMessages
            .AsNoTracking()
            .Where(x => x.DeviceId == deviceId)
            .Select(x => new PrinterTimelineItemDto
            {
                Source = "message",
                EventKind = "message",
                TimestampUtc = x.CreateTimeUtc ?? x.CreatedAtUtc,
                ExternalTaskId = x.ExternalTaskId,
                ExternalMessageId = x.ExternalMessageId,
                Title = x.Title ?? $"Message {x.ExternalMessageId}",
                Detail = x.Detail
            })
            .ToListAsync(cancellationToken);

        return tasks
            .Concat(messages)
            .OrderByDescending(x => x.TimestampUtc)
            .ToList();
    }

    public async Task<bool> CanStreamTelemetryAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var printer = await _db.Printers
            .AsNoTracking()
            .Include(x => x.SimulationControl)
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (printer is null)
            return false;

        if (!string.Equals(printer.PrintStatus, "RUNNING", StringComparison.OrdinalIgnoreCase))
            return false;

        if (printer.SimulationControl is null)
            return false;

        if (!printer.SimulationControl.IsLocked)
            return false;

        if (!string.Equals(printer.SimulationControl.SimulationState, "RUNNING", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!printer.SimulationControl.ActivePrinterTaskId.HasValue)
            return false;

        return true;
    }

    private static bool IsSimulationLocked(Printer p, DateTimeOffset now)
    {
        return p.SimulationControl is not null &&
            p.SimulationControl.IsLocked &&
            p.SimulationControl.LockedUntilUtc.HasValue &&
            p.SimulationControl.LockedUntilUtc > now;
    }

    private static string ResolveOperationalState(Printer p, DateTimeOffset now)
    {
        if (IsSimulationLocked(p, now) &&
            !string.IsNullOrWhiteSpace(p.SimulationControl!.SimulationState))
        {
            return p.SimulationControl.SimulationState!;
        }

        if (!p.IsOnline)
            return "OFFLINE";

        return p.PrintStatus switch
        {
            "RUNNING" => "RUNNING",
            "FAIL" => "FAIL",
            "SUCCESS" => "SUCCESS",
            "ACTIVE" => "IDLE",
            _ => "IDLE"
        };
    }

    private static PrinterListItemDto MapPrinterListItem(Printer p, DateTimeOffset now)
    {
        var operationalState = ResolveOperationalState(p, now);
        var isSimulationControlled = IsSimulationLocked(p, now);

        return new PrinterListItemDto
        {
            DeviceId = p.DeviceId,
            Name = p.Name,
            IsOnline = p.IsOnline,
            PrintStatus = p.PrintStatus,
            ModelName = p.ModelName,
            ProductName = p.ProductName,
            Structure = p.Structure,
            NozzleDiameterMm = p.NozzleDiameterMm,
            LastBindSyncAtUtc = p.LastBindSyncAtUtc,
            AmsUnitCount = p.AmsUnits.Count,
            CurrentFirmwareVersion = p.FirmwareStatus?.CurrentVersion,
            LatestFirmwareVersion = p.FirmwareStatus?.LatestVersion,
            ForceUpdate = p.FirmwareStatus?.ForceUpdate,
            LastVersionSyncAtUtc = p.FirmwareStatus?.LastVersionSyncAtUtc,
            OperationalState = operationalState,
            IsRunning = string.Equals(operationalState, "RUNNING", StringComparison.OrdinalIgnoreCase),
            IsSimulationControlled = isSimulationControlled
        };
    }
}
