using DigitalTwin.Application.Abstractions.Telemetry;
using DigitalTwin.Application.Telemetry.Models;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Queries;

public class TaskTelemetrySummaryService
{
    private readonly DigitalTwinDbContext _db;
    private readonly IPrinterTelemetryWriter _telemetryWriter;

    public TaskTelemetrySummaryService(
        DigitalTwinDbContext db,
        IPrinterTelemetryWriter telemetryWriter)
    {
        _db = db;
        _telemetryWriter = telemetryWriter;
    }

    public async Task<TaskTelemetrySummaryDto?> GetTaskSummaryAsync(
        long externalTaskId,
        CancellationToken cancellationToken = default)
    {
        var task = await _db.PrinterTasks
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ExternalTaskId == externalTaskId, cancellationToken);

        if (task is null)
            return null;

        if (!task.StartTimeUtc.HasValue)
        {
            return new TaskTelemetrySummaryDto
            {
                ExternalTaskId = task.ExternalTaskId,
                TaskAlias = task.TaskAlias,
                DeviceId = task.DeviceId,
                DesignTitle = task.DesignTitle,
                StatusText = task.StatusText ?? "UNKNOWN",
                StartTimeUtc = task.StartTimeUtc,
                EndTimeUtc = task.EndTimeUtc,
                HasTelemetry = false,
                Message = "Task exists but has no start time, so telemetry range cannot be determined."
            };
        }

        var endUtc = task.EndTimeUtc ?? DateTimeOffset.UtcNow;

        var points = await _telemetryWriter.QueryTaskRangeAsync(
            task.DeviceId,
            task.ExternalTaskId,
            task.StartTimeUtc.Value,
            endUtc,
            cancellationToken);

        if (points.Count == 0)
        {
            return new TaskTelemetrySummaryDto
            {
                ExternalTaskId = task.ExternalTaskId,
                TaskAlias = task.TaskAlias,
                DeviceId = task.DeviceId,
                DesignTitle = task.DesignTitle,
                StatusText = task.StatusText ?? "UNKNOWN",
                StartTimeUtc = task.StartTimeUtc,
                EndTimeUtc = task.EndTimeUtc,
                HasTelemetry = false,
                Message = "No telemetry points found in InfluxDB for this printer/task time range."
            };
        }

        var latest = points.OrderByDescending(x => x.TimestampUtc).First();

        return new TaskTelemetrySummaryDto
        {
            ExternalTaskId = task.ExternalTaskId,
            TaskAlias = task.TaskAlias,
            DeviceId = task.DeviceId,
            DesignTitle = task.DesignTitle,
            StatusText = task.StatusText ?? "UNKNOWN",
            StartTimeUtc = task.StartTimeUtc,
            EndTimeUtc = task.EndTimeUtc,
            HasTelemetry = true,

            LatestProgressPercent = latest.ProgressPercent,
            LatestCurrentLayer = latest.CurrentLayer,
            LatestTotalLayers = latest.TotalLayers,

            AvgNozzleTempC = Math.Round(points.Average(x => x.NozzleTempC), 2),
            AvgBedTempC = Math.Round(points.Average(x => x.BedTempC), 2),
            AvgChamberTempC = Math.Round(points.Average(x => x.ChamberTempC), 2),

            LatestFilamentRemainingGrams = latest.FilamentRemainingGrams,
            LatestPowerWatts = latest.PowerWatts,
            LatestVibrationScore = latest.VibrationScore,

            Message = "Task telemetry summary generated successfully."
        };
    }
}
