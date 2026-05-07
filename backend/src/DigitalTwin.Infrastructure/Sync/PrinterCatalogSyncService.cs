using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigitalTwin.Application.Abstractions.Caching;
using DigitalTwin.Application.Abstractions.External;
using DigitalTwin.Application.Printers.Dtos;
using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Sync;

public class PrinterCatalogSyncService
{
    private readonly IBambuProxyClient _proxyClient;
    private readonly IFleetCache _fleetCache;
    private readonly DigitalTwinDbContext _db;

    public PrinterCatalogSyncService(
        IBambuProxyClient proxyClient,
        IFleetCache fleetCache,
        DigitalTwinDbContext db)
    {
        _proxyClient = proxyClient;
        _fleetCache = fleetCache;
        _db = db;
    }

    public async Task SyncBindAsync(CancellationToken cancellationToken = default)
    {
        var response = await _proxyClient.GetBindAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var dto in response.Devices)
        {
            var bindSignature = ComputeSignature(dto);
            var cacheKey = $"bind:{dto.DeviceId}";
            var existingSignature = await _fleetCache.GetSignatureAsync(cacheKey, cancellationToken);

            if (existingSignature == bindSignature)
                continue;

            var printer = await _db.Printers
                .Include(x => x.SimulationControl)
                .AsTracking()
                .FirstOrDefaultAsync(x => x.DeviceId == dto.DeviceId, cancellationToken);

            if (printer is null)
            {
                printer = new Printer
                {
                    Id = Guid.NewGuid(),
                    DeviceId = dto.DeviceId,
                    CreatedAtUtc = now
                };

                _db.Printers.Add(printer);
            }

            printer.Name = !string.IsNullOrWhiteSpace(dto.Name)
                ? dto.Name
                : dto.DeviceId;

            printer.ModelName = dto.ModelName;
            printer.ProductName = dto.ProductName;
            printer.Structure = dto.Structure;
            printer.NozzleDiameterMm = dto.NozzleDiameter;
            printer.IsAmsSupported = dto.Ams is { Count: > 0 };
            printer.LastBindSyncAtUtc = now;
            printer.UpdatedAtUtc = now;

            if (!IsSimulationLocked(printer, now))
            {
                printer.IsOnline = dto.IsOnline;
                printer.PrintStatus = dto.PrintStatus;
            }

            await UpsertAmsFromBindExplicitAsync(printer.Id, dto, now, cancellationToken);

            await _fleetCache.SetSignatureAsync(
                cacheKey,
                bindSignature,
                TimeSpan.FromHours(1),
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncVersionAsync(CancellationToken cancellationToken = default)
    {
        var response = await _proxyClient.GetVersionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;

        foreach (var dto in response.Devices)
        {
            var versionSignature = ComputeSignature(dto);
            var cacheKey = $"version:{dto.DeviceId}";
            var existingSignature = await _fleetCache.GetSignatureAsync(cacheKey, cancellationToken);

            if (existingSignature == versionSignature)
                continue;

            var printer = await _db.Printers
                .AsTracking()
                .FirstOrDefaultAsync(x => x.DeviceId == dto.DeviceId, cancellationToken);

            if (printer is null)
                continue;

            var latestFirmware = dto.Firmware?.FirstOrDefault();

            var firmwareStatus = await _db.PrinterFirmwareStatuses
                .FirstOrDefaultAsync(x => x.PrinterId == printer.Id, cancellationToken);

            if (firmwareStatus is null)
            {
                firmwareStatus = new PrinterFirmwareStatus
                {
                    Id = Guid.NewGuid(),
                    PrinterId = printer.Id,
                    CreatedAtUtc = now
                };

                _db.PrinterFirmwareStatuses.Add(firmwareStatus);
            }

            firmwareStatus.CurrentVersion = dto.CurrentVersion;
            firmwareStatus.LatestVersion = latestFirmware?.Version;
            firmwareStatus.ForceUpdate = latestFirmware?.ForceUpdate;
            firmwareStatus.ReleaseStatus = latestFirmware?.Status;
            firmwareStatus.Description = latestFirmware?.Description;
            firmwareStatus.DownloadUrl = latestFirmware?.Url;
            firmwareStatus.LastVersionSyncAtUtc = now;
            firmwareStatus.UpdatedAtUtc = now;

            await UpsertAmsFromVersionExplicitAsync(printer.Id, dto, now, cancellationToken);

            await _fleetCache.SetSignatureAsync(
                cacheKey,
                versionSignature,
                TimeSpan.FromHours(1),
                cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAmsFromBindExplicitAsync(
        Guid printerId,
        BindPrinterDto dto,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (dto.Ams is null || dto.Ams.Count == 0)
            return;

        for (var i = 0; i < dto.Ams.Count; i++)
        {
            var amsDto = dto.Ams[i];

            var unit = await _db.PrinterAmsUnits
                .AsTracking()
                .FirstOrDefaultAsync(
                    x => x.PrinterId == printerId && x.AmsIndex == i,
                    cancellationToken);

            if (unit is null)
            {
                unit = new PrinterAmsUnit
                {
                    Id = Guid.NewGuid(),
                    PrinterId = printerId,
                    AmsIndex = i,
                    CreatedAtUtc = now
                };

                _db.PrinterAmsUnits.Add(unit);
            }

            unit.HumidityLevel = TryParseInt(amsDto.Humidity);
            unit.TemperatureCelsius = TryParseDecimal(amsDto.Temperature);
            unit.LastSeenAtUtc = now;
            unit.UpdatedAtUtc = now;
        }
    }

    private async Task UpsertAmsFromVersionExplicitAsync(
        Guid printerId,
        VersionPrinterDto dto,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (dto.Ams is null || dto.Ams.Count == 0)
            return;

        for (var i = 0; i < dto.Ams.Count; i++)
        {
            var amsDto = dto.Ams[i];
            var latestFirmware = amsDto.Firmware?.FirstOrDefault();

            var unit = await _db.PrinterAmsUnits
                .AsTracking()
                .FirstOrDefaultAsync(
                    x => x.PrinterId == printerId && x.AmsIndex == i,
                    cancellationToken);

            if (unit is null)
            {
                unit = new PrinterAmsUnit
                {
                    Id = Guid.NewGuid(),
                    PrinterId = printerId,
                    AmsIndex = i,
                    CreatedAtUtc = now
                };

                _db.PrinterAmsUnits.Add(unit);
            }

            unit.AmsDeviceId = amsDto.DeviceId;
            unit.CurrentVersion = amsDto.CurrentVersion;
            unit.LatestVersion = latestFirmware?.Version;
            unit.ForceUpdate = latestFirmware?.ForceUpdate;
            unit.ReleaseStatus = latestFirmware?.Status;
            unit.Description = latestFirmware?.Description;
            unit.DownloadUrl = latestFirmware?.Url;
            unit.LastSeenAtUtc = now;
            unit.UpdatedAtUtc = now;
        }
    }

    private static bool IsSimulationLocked(Printer printer, DateTimeOffset now)
    {
        return printer.SimulationControl is not null &&
            printer.SimulationControl.IsLocked &&
            printer.SimulationControl.LockedUntilUtc.HasValue &&
            printer.SimulationControl.LockedUntilUtc > now;
    }

    private static int? TryParseInt(string? value)
        => int.TryParse(value, out var parsed) ? parsed : null;

    private static decimal? TryParseDecimal(string? value)
        => decimal.TryParse(value, out var parsed) ? parsed : null;

    private static string ComputeSignature<T>(T obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(bytes);
    }
}