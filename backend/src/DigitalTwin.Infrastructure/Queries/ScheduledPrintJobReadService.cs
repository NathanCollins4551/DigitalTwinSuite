using DigitalTwin.Application.Printers.Dtos;
using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Queries;

public class ScheduledPrintJobReadService
{
    private readonly DigitalTwinDbContext _db;

    private static readonly string[] MaterialTypes =
    {
        "PLA", "PETG", "ABS", "TPU", "PA-CF"
    };

    private static readonly string[] Colors =
    {
        "#FFFFFF", "#000000", "#FF0000", "#00FF00", "#0000FF",
        "#FFFF00", "#FFA500", "#808080", "#8A2BE2", "#00CED1"
    };

    public ScheduledPrintJobReadService(DigitalTwinDbContext db)
    {
        _db = db;
    }

    public async Task<ScheduledPrintJobDto> CreateAsync(
        CreateScheduledPrintJobRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new InvalidOperationException("FileName is required.");

        if (request.Priority < 0 || request.Priority > 100)
            throw new InvalidOperationException("Priority must be between 0 and 100.");

        if (!request.AllowAnyPrinter && string.IsNullOrWhiteSpace(request.PreferredPrinterDeviceId))
            throw new InvalidOperationException("PreferredPrinterDeviceId is required when AllowAnyPrinter is false.");

        Printer? preferredPrinter = null;
        if (!string.IsNullOrWhiteSpace(request.PreferredPrinterDeviceId))
        {
            preferredPrinter = await _db.Printers
                .FirstOrDefaultAsync(x => x.DeviceId == request.PreferredPrinterDeviceId, cancellationToken);

            if (preferredPrinter is null)
                throw new InvalidOperationException("Preferred printer was not found.");
        }

        var now = DateTimeOffset.UtcNow;

        var displayName = BuildDisplayNameFromFileName(request.FileName);

        var estimatedDurationSeconds = EstimateDurationSecondsFromFileName(request.FileName);
        var estimatedFilamentGrams = EstimateFilamentGramsFromFileName(request.FileName);

        var materialType = !string.IsNullOrWhiteSpace(request.RequestedMaterialType)
            ? request.RequestedMaterialType
            : MaterialTypes[Random.Shared.Next(MaterialTypes.Length)];

        var color = !string.IsNullOrWhiteSpace(request.RequestedColor)
            ? request.RequestedColor
            : Colors[Random.Shared.Next(Colors.Length)];

        var job = new ScheduledPrintJob
        {
            Id = Guid.NewGuid(),
            FileName = request.FileName.Trim(),
            DisplayName = displayName,
            Priority = request.Priority,
            PreferredPrinterId = preferredPrinter?.Id,
            AllowAnyPrinter = request.AllowAnyPrinter,
            Status = "QUEUED",
            RequestedMaterialType = materialType,
            RequestedColor = color,
            EstimatedDurationSeconds = estimatedDurationSeconds,
            EstimatedFilamentGrams = estimatedFilamentGrams,
            RequestedStartAfterUtc = request.RequestedStartAfterUtc,
            DueAtUtc = request.DueAtUtc,
            Notes = request.Notes,
            IsSimulatedInput = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.ScheduledPrintJobs.Add(job);
        await _db.SaveChangesAsync(cancellationToken);

        return await GetByIdOrThrowAsync(job.Id, cancellationToken);
    }

    public async Task<List<ScheduledPrintJobDto>> GetAllAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ScheduledPrintJobs
            .AsNoTracking()
            .Include(x => x.PreferredPrinter)
            .Include(x => x.AssignedPrinter)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAtUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        var items = await query.ToListAsync(cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<ScheduledPrintJobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _db.ScheduledPrintJobs
            .AsNoTracking()
            .Include(x => x.PreferredPrinter)
            .Include(x => x.AssignedPrinter)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return item is null ? null : MapToDto(item);
    }

    public async Task<(bool Success, string Message)> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _db.ScheduledPrintJobs
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (job is null)
            return (false, "Scheduled print job not found.");

        if (job.Status is "COMPLETED" or "FAILED" or "CANCELLED")
            return (false, $"Cannot cancel a job in status '{job.Status}'.");

        job.Status = "CANCELLED";
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return (true, "Scheduled print job cancelled successfully.");
    }

    public async Task<(bool Success, string Message, ScheduledPrintJobDto? Job)> UpdatePriorityAsync(
        Guid id,
        int priority,
        CancellationToken cancellationToken = default)
    {
        if (priority < 0 || priority > 100)
            return (false, "Priority must be between 0 and 100.", null);

        var job = await _db.ScheduledPrintJobs
            .Include(x => x.PreferredPrinter)
            .Include(x => x.AssignedPrinter)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (job is null)
            return (false, "Scheduled print job not found.", null);

        if (job.Status is "COMPLETED" or "FAILED" or "CANCELLED")
            return (false, $"Cannot change priority for a job in status '{job.Status}'.", null);

        job.Priority = priority;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return (true, "Priority updated successfully.", MapToDto(job));
    }

    public async Task<List<ScheduledPrintJobPreviewDto>> GetQueuePreviewAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _db.ScheduledPrintJobs
            .AsNoTracking()
            .Include(x => x.PreferredPrinter)
            .Where(x => x.Status == "QUEUED")
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueAtUtc)
            .ThenBy(x => x.CreatedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        return jobs.Select(x => new ScheduledPrintJobPreviewDto
        {
            Id = x.Id,
            FileName = x.FileName,
            DisplayName = x.DisplayName,
            Priority = x.Priority,
            Status = x.Status,
            PreferredPrinterDeviceId = x.PreferredPrinter?.DeviceId,
            AllowAnyPrinter = x.AllowAnyPrinter,
            EstimatedDurationSeconds = x.EstimatedDurationSeconds,
            CompatibilityNote = x.CompatibilityNote,
            EstimatedStartAtUtc = x.EstimatedStartAtUtc,
            EstimatedFinishAtUtc = x.EstimatedFinishAtUtc
        }).ToList();
    }

    // public async Task<SchedulerControlDto> GetSchedulerControlAsync(CancellationToken cancellationToken = default)
    // {
    //     var control = await GetOrCreateSchedulerControlAsync(cancellationToken);

    //     return new SchedulerControlDto
    //     {
    //         IsPaused = control.IsPaused,
    //         PauseReason = control.PauseReason,
    //         UpdatedAtUtc = control.UpdatedAtUtc
    //     };
    // }

    // public async Task<SchedulerControlDto> UpdateSchedulerControlAsync(
    //     bool isPaused,
    //     string? pauseReason,
    //     CancellationToken cancellationToken = default)
    // {
    //     var control = await GetOrCreateSchedulerControlAsync(cancellationToken);

    //     control.IsPaused = isPaused;
    //     control.PauseReason = pauseReason;
    //     control.UpdatedAtUtc = DateTimeOffset.UtcNow;

    //     await _db.SaveChangesAsync(cancellationToken);

    //     return new SchedulerControlDto
    //     {
    //         IsPaused = control.IsPaused,
    //         PauseReason = control.PauseReason,
    //         UpdatedAtUtc = control.UpdatedAtUtc
    //     };
    // }

    // private async Task<SchedulerControl> GetOrCreateSchedulerControlAsync(CancellationToken cancellationToken)
    // {
    //     var control = await _db.SchedulerControls.FirstOrDefaultAsync(cancellationToken);

    //     if (control is not null)
    //         return control;

    //     control = new SchedulerControl
    //     {
    //         Id = Guid.NewGuid(),
    //         IsPaused = false,
    //         PauseReason = null,
    //         CreatedAtUtc = DateTimeOffset.UtcNow,
    //         UpdatedAtUtc = DateTimeOffset.UtcNow
    //     };

    //     _db.SchedulerControls.Add(control);
    //     await _db.SaveChangesAsync(cancellationToken);

    //     return control;
    // }

    private async Task<ScheduledPrintJobDto> GetByIdOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var dto = await GetByIdAsync(id, cancellationToken);
        if (dto is null)
            throw new InvalidOperationException("Scheduled print job was created but could not be reloaded.");
        return dto;
    }

    private static ScheduledPrintJobDto MapToDto(ScheduledPrintJob x)
    {
        return new ScheduledPrintJobDto
        {
            Id = x.Id,
            FileName = x.FileName,
            DisplayName = x.DisplayName,
            Priority = x.Priority,

            PreferredPrinterId = x.PreferredPrinterId,
            PreferredPrinterDeviceId = x.PreferredPrinter?.DeviceId,
            PreferredPrinterName = x.PreferredPrinter?.Name,

            AllowAnyPrinter = x.AllowAnyPrinter,
            Status = x.Status,

            AssignedPrinterId = x.AssignedPrinterId,
            AssignedPrinterDeviceId = x.AssignedPrinter?.DeviceId,
            AssignedPrinterName = x.AssignedPrinter?.Name,

            StartedPrinterTaskId = x.StartedPrinterTaskId,

            RequestedMaterialType = x.RequestedMaterialType,
            RequestedColor = x.RequestedColor,

            EstimatedDurationSeconds = x.EstimatedDurationSeconds,
            EstimatedFilamentGrams = x.EstimatedFilamentGrams,

            RequestedStartAfterUtc = x.RequestedStartAfterUtc,
            DueAtUtc = x.DueAtUtc,
            Notes = x.Notes,

            IsSimulatedInput = x.IsSimulatedInput,

            SchedulerDecisionReason = x.SchedulerDecisionReason,
            CompatibilityNote = x.CompatibilityNote,
            EstimatedStartAtUtc = x.EstimatedStartAtUtc,
            EstimatedFinishAtUtc = x.EstimatedFinishAtUtc,

            CreatedAtUtc = x.CreatedAtUtc,
            UpdatedAtUtc = x.UpdatedAtUtc
        };
    }

    private static string BuildDisplayNameFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName).Trim();

        if (string.IsNullOrWhiteSpace(name))
            return "Untitled Print Job";

        var normalized = name.Replace("_", " ").Replace("-", " ");
        var words = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant());

        return string.Join(" ", words);
    }

    private static int EstimateDurationSecondsFromFileName(string fileName)
    {
        var seed = Math.Abs(fileName.ToLowerInvariant().GetHashCode());

        var baseMinutes = 10 + (seed % 111); // 10 to 120 minutes
        return baseMinutes * 60;
    }

    private static decimal EstimateFilamentGramsFromFileName(string fileName)
    {
        var seed = Math.Abs((fileName.ToLowerInvariant() + "-filament").GetHashCode());

        var value = 10m + (seed % 191); // 10 to 200g
        return Math.Round(value + ((seed % 100) / 100m), 2);
    }
}