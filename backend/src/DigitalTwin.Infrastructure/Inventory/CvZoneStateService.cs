using System.Text.Json;
using DigitalTwin.Application.Abstractions.Inventory;
using DigitalTwin.Application.Inventory.Dtos;
using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Inventory;

public class CvZoneStateService
{
    private readonly DigitalTwinDbContext _db;
    private readonly IZoneInventoryPublisher _publisher;

    public CvZoneStateService(
        DigitalTwinDbContext db,
        IZoneInventoryPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task UpsertInventoryMessageAsync(
        CvZoneInventoryMessageDto message,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // 1. Update cv_zone_states summary rows
        foreach (var zoneEntry in message.Zones)
        {
            var zoneName = zoneEntry.Key;
            var zone = zoneEntry.Value;

            var state = await _db.CvZoneStates.FirstOrDefaultAsync(
                x => x.CameraId == message.CameraId && x.ZoneName == zoneName,
                cancellationToken);

            if (state is null)
            {
                state = new CvZoneState
                {
                    Id = Guid.NewGuid(),
                    CameraId = message.CameraId,
                    ZoneName = zoneName,
                    CreatedAtUtc = now
                };

                _db.CvZoneStates.Add(state);
            }

            state.SpoolIdsJson = JsonSerializer.Serialize(zone.SpoolIds);
            state.UnknownSpoolCount = zone.UnknownSpoolCount;
            state.LastInventoryTsUtc = message.Ts;
            state.UpdatedAtUtc = now;
        }

        // 2. Replace current normalized outside-printer spools for this camera
        var existingSpools = await _db.CvZoneSpools
            .Where(x => x.CameraId == message.CameraId)
            .ToListAsync(cancellationToken);

        _db.CvZoneSpools.RemoveRange(existingSpools);

        var newRows = new List<CvZoneSpool>();

        foreach (var zoneEntry in message.Zones)
        {
            var zoneName = zoneEntry.Key;
            var zone = zoneEntry.Value;

            foreach (var spoolCode in zone.SpoolIds.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var parsed = TryParseSpoolCode(spoolCode);
                if (parsed is null)
                    continue;

                newRows.Add(new CvZoneSpool
                {
                    Id = Guid.NewGuid(),
                    CameraId = message.CameraId,
                    ZoneName = zoneName,
                    SpoolCode = spoolCode,
                    MaterialType = parsed.Value.MaterialType,
                    ColorName = parsed.Value.ColorName,
                    ColorHex = parsed.Value.ColorHex,
                    LastSeenAtUtc = message.Ts,
                    UpdatedAtUtc = now,
                    CreatedAtUtc = now
                });
            }
        }

        if (newRows.Count > 0)
            _db.CvZoneSpools.AddRange(newRows);

        await _db.SaveChangesAsync(cancellationToken);

        var snapshot = await GetSnapshotAsync(message.CameraId, cancellationToken);
        _publisher.Publish(snapshot);
    }

    public async Task UpsertAnomalyMessageAsync(
        CvZoneAnomalyMessageDto message,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var zoneEntry in message.Zones)
        {
            var zoneName = zoneEntry.Key;
            var zone = zoneEntry.Value;

            var state = await _db.CvZoneStates.FirstOrDefaultAsync(
                x => x.CameraId == message.CameraId && x.ZoneName == zoneName,
                cancellationToken);

            if (state is null)
            {
                state = new CvZoneState
                {
                    Id = Guid.NewGuid(),
                    CameraId = message.CameraId,
                    ZoneName = zoneName,
                    SpoolIdsJson = "[]",
                    CreatedAtUtc = now
                };

                _db.CvZoneStates.Add(state);
            }

            state.OtherObjectCount = zone.OtherCount;
            state.LastAnomalyTsUtc = message.Ts;
            state.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var snapshot = await GetSnapshotAsync(message.CameraId, cancellationToken);
        _publisher.Publish(snapshot);
    }

    public async Task<ZoneSnapshotResponseDto> GetSnapshotAsync(
        string cameraId,
        CancellationToken cancellationToken = default)
    {
        var states = await _db.CvZoneStates
            .AsNoTracking()
            .Where(x => x.CameraId == cameraId)
            .OrderBy(x => x.ZoneName)
            .ToListAsync(cancellationToken);

        var zoneSpools = await _db.CvZoneSpools
            .AsNoTracking()
            .Where(x => x.CameraId == cameraId)
            .ToListAsync(cancellationToken);

        var spoolGroups = zoneSpools
            .GroupBy(x => x.ZoneName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var zoneMap = new Dictionary<string, ZoneViewDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var state in states)
        {
            var colors = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (spoolGroups.TryGetValue(state.ZoneName, out var spoolsInZone))
            {
                foreach (var spool in spoolsInZone)
                {
                    colors[spool.ColorName] = colors.TryGetValue(spool.ColorName, out var count)
                        ? count + 1
                        : 1;
                }
            }

            zoneMap[state.ZoneName] = new ZoneViewDto
            {
                Colors = colors,
                UnknownSpoolCount = state.UnknownSpoolCount,
                OtherObjectCount = state.OtherObjectCount,
                UpdatedAtUtc = state.UpdatedAtUtc
            };
        }

        return new ZoneSnapshotResponseDto
        {
            CameraId = cameraId,
            Zones = zoneMap
        };
    }

    public async Task<ZoneSnapshotResponseDto?> GetLatestSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var latestCameraId = await _db.CvZoneStates
            .AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(x => x.CameraId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(latestCameraId))
            return null;

        return await GetSnapshotAsync(latestCameraId, cancellationToken);
    }

    public async Task<List<CvZoneSpool>> GetInventorySpoolsAsync(
        string? materialType,
        string? colorName,
        CancellationToken cancellationToken = default)
    {
        var query = _db.CvZoneSpools
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(materialType))
            query = query.Where(x => x.MaterialType == materialType);

        if (!string.IsNullOrWhiteSpace(colorName))
            query = query.Where(x => x.ColorName == colorName);

        return await query
            .OrderBy(x => x.ZoneName)
            .ThenBy(x => x.ColorName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PrinterLoadedSpoolDto>> GetPrinterLoadedSpoolsAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var printer = await _db.Printers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (printer is null)
            return new List<PrinterLoadedSpoolDto>();

        return await _db.PrinterLoadedSpools
            .AsNoTracking()
            .Where(x => x.PrinterId == printer.Id && x.IsActive)
            .OrderBy(x => x.SlotIndex)
            .Select(x => new PrinterLoadedSpoolDto
            {
                Id = x.Id,
                DeviceId = printer.DeviceId,
                PrinterName = printer.Name,
                SlotIndex = x.SlotIndex,
                SpoolCode = x.SpoolCode,
                MaterialType = x.MaterialType,
                ColorName = x.ColorName,
                ColorHex = x.ColorHex,
                RemainingPercent = x.RemainingPercent,
                RemainingGrams = x.RemainingGrams,
                IsActive = x.IsActive,
                UpdatedAtUtc = x.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PrinterReplacementSuggestionDto?> GetReplacementSuggestionsAsync(
        string deviceId,
        decimal lowThresholdPercent = 15m,
        CancellationToken cancellationToken = default)
    {
        var printer = await _db.Printers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (printer is null)
            return null;

        var loadedSpools = await _db.PrinterLoadedSpools
            .AsNoTracking()
            .Where(x => x.PrinterId == printer.Id && x.IsActive)
            .OrderBy(x => x.SlotIndex)
            .ToListAsync(cancellationToken);

        var inventorySpools = await _db.CvZoneSpools
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var result = new PrinterReplacementSuggestionDto
        {
            DeviceId = deviceId
        };

        foreach (var loaded in loadedSpools)
        {
            if (!loaded.RemainingPercent.HasValue || loaded.RemainingPercent.Value > lowThresholdPercent)
                continue;

            var matches = inventorySpools
                .Where(x =>
                    x.MaterialType.Equals(loaded.MaterialType, StringComparison.OrdinalIgnoreCase) &&
                    x.ColorName.Equals(loaded.ColorName, StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.ZoneName, StringComparer.OrdinalIgnoreCase)
                .Select(g => new InventoryZoneMatchDto
                {
                    ZoneName = g.Key,
                    MatchingSpoolCodes = g.Select(x => x.SpoolCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                })
                .OrderBy(x => x.ZoneName)
                .ToList();

            result.Replacements.Add(new PrinterReplacementItemDto
            {
                SlotIndex = loaded.SlotIndex,
                CurrentSpoolCode = loaded.SpoolCode,
                MaterialType = loaded.MaterialType,
                ColorName = loaded.ColorName,
                ColorHex = loaded.ColorHex,
                RemainingPercent = loaded.RemainingPercent,
                RemainingGrams = loaded.RemainingGrams,
                SuggestedZones = matches
            });
        }

        return result;
    }

    public async Task<List<PrinterLoadedSpoolDto>?> UpdatePrinterLoadedSpoolsAsync(
        string deviceId,
        UpdatePrinterLoadedSpoolsRequest request,
        CancellationToken cancellationToken = default)
    {
        var printer = await _db.Printers
            .AsNoTracking()
            .Include(x => x.AmsUnits)
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (printer is null)
            return null;

        if (request.LoadedSpools is null)
            throw new InvalidOperationException("LoadedSpools is required.");

        var duplicateSlot = request.LoadedSpools
            .GroupBy(x => x.SlotIndex)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateSlot is not null)
            throw new InvalidOperationException($"Duplicate SlotIndex detected: {duplicateSlot.Key}");

        foreach (var item in request.LoadedSpools)
        {
            if (item.SlotIndex < 0)
                throw new InvalidOperationException("SlotIndex must be >= 0.");

            if (string.IsNullOrWhiteSpace(item.SpoolCode))
                throw new InvalidOperationException("SpoolCode is required.");

            if (string.IsNullOrWhiteSpace(item.MaterialType))
                throw new InvalidOperationException("MaterialType is required.");

            if (string.IsNullOrWhiteSpace(item.ColorName))
                throw new InvalidOperationException("ColorName is required.");

            if (string.IsNullOrWhiteSpace(item.ColorHex))
                throw new InvalidOperationException("ColorHex is required.");

            if (item.RemainingPercent.HasValue &&
                (item.RemainingPercent.Value < 0 || item.RemainingPercent.Value > 100))
            {
                throw new InvalidOperationException("RemainingPercent must be between 0 and 100.");
            }

            if (item.PrinterAmsUnitId.HasValue &&
                !printer.AmsUnits.Any(x => x.Id == item.PrinterAmsUnitId.Value))
            {
                throw new InvalidOperationException(
                    $"PrinterAmsUnitId '{item.PrinterAmsUnitId.Value}' does not belong to printer '{deviceId}'.");
            }
        }

        var now = DateTimeOffset.UtcNow;

        var existing = await _db.PrinterLoadedSpools
            .Where(x => x.PrinterId == printer.Id && x.IsActive)
            .ToListAsync(cancellationToken);

        // Replace current active loaded spools for this printer
        foreach (var row in existing)
        {
            row.IsActive = false;
            row.UpdatedAtUtc = now;
        }

        foreach (var item in request.LoadedSpools)
        {
            _db.PrinterLoadedSpools.Add(new PrinterLoadedSpool
            {
                Id = Guid.NewGuid(),
                PrinterId = printer.Id,
                PrinterAmsUnitId = item.PrinterAmsUnitId,
                SlotIndex = item.SlotIndex,
                SpoolCode = item.SpoolCode.Trim(),
                MaterialType = item.MaterialType.Trim().ToUpperInvariant(),
                ColorName = item.ColorName.Trim().ToLowerInvariant(),
                ColorHex = item.ColorHex.Trim().ToUpperInvariant(),
                RemainingPercent = item.RemainingPercent,
                RemainingGrams = item.RemainingGrams,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return await GetPrinterLoadedSpoolsAsync(deviceId, cancellationToken);
    }

    private static (string MaterialType, string ColorName, string ColorHex)? TryParseSpoolCode(string spoolCode)
    {
        if (string.IsNullOrWhiteSpace(spoolCode))
            return null;

        var parts = spoolCode
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Example: FIL-PETG-RED
        if (parts.Length < 3)
            return null;

        var materialType = parts[1].ToUpperInvariant();
        var colorName = parts[^1].ToLowerInvariant();
        var colorHex = ColorNameToHex(colorName);

        return (materialType, colorName, colorHex);
    }

    private static string ColorNameToHex(string colorName)
    {
        return colorName.ToLowerInvariant() switch
        {
            "red" => "#FF0000",
            "blue" => "#0000FF",
            "black" => "#000000",
            "green" => "#00FF00",
            _ => "#808080"
        };
    }
}