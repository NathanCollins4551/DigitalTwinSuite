using System.Text.Json;
using DigitalTwin.Application.Abstractions.Caching;
using DigitalTwin.Application.Abstractions.External;
using DigitalTwin.Application.Printers.Dtos;
using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Sync;

public class PrinterActivitySyncService
{
    private readonly IBambuProxyClient _proxyClient;
    private readonly IFleetCache _fleetCache;
    private readonly DigitalTwinDbContext _db;

    public PrinterActivitySyncService(
        IBambuProxyClient proxyClient,
        IFleetCache fleetCache,
        DigitalTwinDbContext db)
    {
        _proxyClient = proxyClient;
        _fleetCache = fleetCache;
        _db = db;
    }

    public async Task SyncTasksAsync(CancellationToken cancellationToken = default)
    {
        var response = await _proxyClient.GetTasksAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var dto in response.Hits)
        {
            var cacheKey = $"task:{dto.Id}";
            var signature = JsonSerializer.Serialize(dto);
            var oldSignature = await _fleetCache.GetSignatureAsync(cacheKey, cancellationToken);

            if (oldSignature == signature)
                continue;

            var printer = await _db.Printers
                .Include(x => x.SimulationControl)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.DeviceId == dto.DeviceId, cancellationToken);

            if (printer?.SimulationControl is not null &&
                printer.SimulationControl.IsLocked &&
                printer.SimulationControl.LockedUntilUtc.HasValue &&
                printer.SimulationControl.LockedUntilUtc > now)
            {
                continue;
            }

            var task = await _db.PrinterTasks
                .AsTracking()
                .FirstOrDefaultAsync(x => x.ExternalTaskId == dto.Id, cancellationToken);

            if (task is null)
            {
                task = new PrinterTask
                {
                    Id = Guid.NewGuid(),
                    ExternalTaskId = dto.Id,
                    TaskAlias = PrinterTask.BuildTaskAlias(dto.DeviceName, dto.DeviceId, dto.Id),
                    CreatedAtUtc = now
                };

                _db.PrinterTasks.Add(task);
            }
            else if (string.IsNullOrWhiteSpace(task.TaskAlias))
            {
                task.TaskAlias = PrinterTask.BuildTaskAlias(dto.DeviceName, dto.DeviceId, dto.Id);
            }

            task.PrinterId = printer?.Id;
            task.DeviceId = dto.DeviceId;
            task.DeviceName = dto.DeviceName;
            task.DeviceModel = dto.DeviceModel;
            task.DesignTitle = dto.DesignTitle;
            task.DesignTitleTranslated = dto.DesignTitleTranslated;
            task.FailedType = dto.FailedType;
            task.Mode = dto.Mode;
            task.BedType = dto.BedType;
            task.CostTimeSeconds = dto.CostTime;
            task.LengthMm = dto.Length;
            task.StartTimeUtc = dto.StartTime;
            task.EndTimeUtc = dto.EndTime;
            task.CoverUrl = dto.Cover;
            task.RawJson = JsonSerializer.Serialize(dto);
            task.SourceUpdatedAtUtc = now;
            task.UpdatedAtUtc = now;

            // Save parent first if new, so child rows have a stable FK
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    Console.WriteLine($"[CONCURRENCY][TASKS] Entity={entry.Entity.GetType().Name}, State={entry.State}");
                }

                throw;
            }

            // Replace AMS detail rows explicitly
            var existingDetails = await _db.PrinterTaskAmsDetails
                .Where(x => x.PrinterTaskId == task.Id)
                .ToListAsync(cancellationToken);

            if (existingDetails.Count > 0)
            {
                _db.PrinterTaskAmsDetails.RemoveRange(existingDetails);
                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var entry in ex.Entries)
                    {
                        Console.WriteLine($"[CONCURRENCY][TASKS] Entity={entry.Entity.GetType().Name}, State={entry.State}");
                    }

                    throw;
                }
            }

            var newDetails = (dto.AmsDetailMapping ?? new List<TaskAmsDetailDto>())
                .Select(detail => new PrinterTaskAmsDetail
                {
                    Id = Guid.NewGuid(),
                    PrinterTaskId = task.Id,
                    Ams = detail.Ams,
                    AmsId = detail.AmsId,
                    SlotId = detail.SlotId,
                    NozzleId = detail.NozzleId,
                    FilamentId = detail.FilamentId,
                    FilamentType = detail.FilamentType,
                    TargetFilamentType = detail.TargetFilamentType,
                    SourceColor = detail.SourceColor,
                    TargetColor = detail.TargetColor,
                    WeightGrams = detail.Weight,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                })
                .ToList();

            if (newDetails.Count > 0)
            {
                _db.PrinterTaskAmsDetails.AddRange(newDetails);
                try
                {
                    await _db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    foreach (var entry in ex.Entries)
                    {
                        Console.WriteLine($"[CONCURRENCY][TASKS] Entity={entry.Entity.GetType().Name}, State={entry.State}");
                    }

                    throw;
                }
            }

            await _fleetCache.SetSignatureAsync(
                cacheKey,
                signature,
                TimeSpan.FromHours(1),
                cancellationToken);
        }
    }

    public async Task SyncMessagesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _proxyClient.GetMessagesAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var dto in response.Hits)
        {
            var cacheKey = $"message:{dto.Id}";
            var signature = JsonSerializer.Serialize(dto);
            var oldSignature = await _fleetCache.GetSignatureAsync(cacheKey, cancellationToken);

            if (oldSignature == signature)
                continue;

            var deviceId = dto.TaskMessage?.DeviceId;
            Printer? printer = null;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                printer = await _db.Printers
                    .Include(x => x.SimulationControl)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);
            }

            if (printer?.SimulationControl is not null &&
                printer.SimulationControl.IsLocked &&
                printer.SimulationControl.LockedUntilUtc.HasValue &&
                printer.SimulationControl.LockedUntilUtc > now)
            {
                continue;
            }

            PrinterTask? relatedTask = null;
            if (dto.TaskMessage?.Id is long extTaskId)
            {
                relatedTask = await _db.PrinterTasks
                    .FirstOrDefaultAsync(x => x.ExternalTaskId == extTaskId, cancellationToken);
            }

            var message = await _db.PrinterMessages
                .FirstOrDefaultAsync(x => x.ExternalMessageId == dto.Id, cancellationToken);

            if (message is null)
            {
                message = new PrinterMessage
                {
                    Id = Guid.NewGuid(),
                    ExternalMessageId = dto.Id,
                    CreatedAtUtc = now
                };
                _db.PrinterMessages.Add(message);
            }

            message.PrinterId = printer?.Id;
            message.RelatedPrinterTaskId = relatedTask?.Id;
            message.ExternalTaskId = dto.TaskMessage?.Id;
            message.Type = dto.Type;
            message.IsRead = dto.IsRead;
            message.CreateTimeUtc = dto.CreateTime;
            message.DeviceId = dto.TaskMessage?.DeviceId;
            message.DeviceName = dto.TaskMessage?.DeviceName;
            message.TaskStatus = dto.TaskMessage?.Status;
            message.Title = dto.TaskMessage?.Title;
            message.Detail = dto.TaskMessage?.Detail;
            message.CoverUrl = dto.TaskMessage?.Cover;
            message.DesignId = dto.TaskMessage?.DesignId;
            message.DesignTitle = dto.TaskMessage?.DesignTitle;
            message.RawJson = JsonSerializer.Serialize(dto);
            message.SourceUpdatedAtUtc = now;
            message.UpdatedAtUtc = now;

            await _fleetCache.SetSignatureAsync(cacheKey, signature, TimeSpan.FromHours(1), cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
