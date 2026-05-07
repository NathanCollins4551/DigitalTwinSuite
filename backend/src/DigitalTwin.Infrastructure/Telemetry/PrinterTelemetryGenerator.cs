using DigitalTwin.Application.Telemetry.Models;
using DigitalTwin.Domain.Entities;

namespace DigitalTwin.Infrastructure.Telemetry;

public class PrinterTelemetryGenerator
{
    public PrinterTelemetryPoint Generate(Printer printer, PrinterTask task, DateTimeOffset now)
    {
        var totalSeconds = Math.Max(1, task.CostTimeSeconds ?? 300);
        var startedAt = task.StartTimeUtc ?? now;
        var elapsed = Math.Max(0, (now - startedAt).TotalSeconds);
        var rawProgress = Math.Min(100m, (decimal)(elapsed / totalSeconds) * 100m);

        var totalLayers = Math.Max(20, (task.LengthMm ?? 10000) / 120);
        var currentLayer = Math.Min(totalLayers, Math.Max(1, (int)Math.Round(rawProgress / 100m * totalLayers)));

        var totalWeight = task.WeightGrams ?? 100m;
        var remaining = Math.Max(0m, totalWeight * (1m - rawProgress / 100m));

        var nozzleBase = 215m;
        var bedBase = 60m;
        var chamberBase = 32m + (rawProgress / 100m) * 5m;

        return new PrinterTelemetryPoint
        {
            DeviceId = printer.DeviceId,
            ExternalTaskId = task.ExternalTaskId,
            TimestampUtc = now,

            PrintStatus = "RUNNING",
            IsSimulated = true,

            ProgressPercent = Math.Round(rawProgress, 2),
            CurrentLayer = currentLayer,
            TotalLayers = totalLayers,

            NozzleTempC = Jitter(nozzleBase, 2.5m),
            BedTempC = Jitter(bedBase, 1.5m),
            ChamberTempC = Jitter(chamberBase, 1.2m),

            PrintSpeedPercent = Random.Shared.Next(80, 121),
            FilamentRemainingGrams = Math.Round(Jitter(remaining, 1.8m), 2),
            PowerWatts = Math.Round(Jitter(145m, 12m), 2),
            VibrationScore = Math.Round(Jitter(0.18m, 0.08m), 3),

            ErrorCode = null
        };
    }

    private static decimal Jitter(decimal center, decimal amplitude)
    {
        var offset = ((decimal)Random.Shared.NextDouble() * 2m - 1m) * amplitude;
        var value = center + offset;
        return value < 0 ? 0 : value;
    }
}