using DigitalTwin.Application.Abstractions.Telemetry;
using DigitalTwin.Application.Telemetry.Models;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Core;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Configuration;

namespace DigitalTwin.Infrastructure.Telemetry;

public class InfluxPrinterTelemetryWriter : IPrinterTelemetryWriter
{
    private readonly InfluxDBClient _client;
    private readonly string _bucket;
    private readonly string _org;

    public InfluxPrinterTelemetryWriter(IConfiguration configuration)
    {
        var url = configuration["InfluxDb:Url"]!;
        var token = configuration["InfluxDb:Token"]!;
        _org = configuration["InfluxDb:Org"]!;
        _bucket = configuration["InfluxDb:Bucket"]!;

        _client = new InfluxDBClient(url, token);
    }

    public async Task WriteAsync(PrinterTelemetryPoint point, CancellationToken cancellationToken = default)
    {
        var writeApi = _client.GetWriteApiAsync();

        var p = PointData
            .Measurement("printer_telemetry")
            .Tag("deviceId", point.DeviceId)
            .Tag("taskId", point.ExternalTaskId.ToString())
            .Tag("printStatus", point.PrintStatus)
            .Tag("isSimulated", point.IsSimulated.ToString().ToLowerInvariant())
            .Field("progressPercent", point.ProgressPercent)
            .Field("currentLayer", point.CurrentLayer)
            .Field("totalLayers", point.TotalLayers)
            .Field("nozzleTempC", point.NozzleTempC)
            .Field("bedTempC", point.BedTempC)
            .Field("chamberTempC", point.ChamberTempC)
            .Field("printSpeedPercent", point.PrintSpeedPercent)
            .Field("filamentRemainingGrams", point.FilamentRemainingGrams)
            .Field("powerWatts", point.PowerWatts)
            .Field("vibrationScore", point.VibrationScore)
            .Field("errorCode", point.ErrorCode ?? string.Empty)
            .Timestamp(point.TimestampUtc.UtcDateTime, WritePrecision.Ns);

        await writeApi.WritePointAsync(p, _bucket, _org, cancellationToken);
    }

    public async Task<IReadOnlyList<PrinterTelemetryPoint>> QueryRecentAsync(
        string deviceId,
        int minutes,
        CancellationToken cancellationToken = default)
    {
        var flux = $"""
        from(bucket: "{_bucket}")
          |> range(start: -{minutes}m)
          |> filter(fn: (r) => r._measurement == "printer_telemetry")
          |> filter(fn: (r) => r.deviceId == "{deviceId}")
          |> pivot(rowKey:["_time"], columnKey: ["_field"], valueColumn: "_value")
          |> sort(columns: ["_time"])
        """;

        var queryApi = _client.GetQueryApi();
        var tables = await queryApi.QueryAsync(flux, _org, cancellationToken);

        var result = new List<PrinterTelemetryPoint>();

        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                result.Add(new PrinterTelemetryPoint
                {
                    DeviceId = record.GetValueByKey("deviceId")?.ToString() ?? deviceId,
                    ExternalTaskId = long.TryParse(record.GetValueByKey("taskId")?.ToString(), out var taskId) ? taskId : 0,
                    TimestampUtc = record.GetTimeInDateTime()?.ToUniversalTime() ?? DateTime.UtcNow,
                    PrintStatus = record.GetValueByKey("printStatus")?.ToString() ?? "UNKNOWN",
                    IsSimulated = bool.TryParse(record.GetValueByKey("isSimulated")?.ToString(), out var sim) && sim,

                    ProgressPercent = ToDecimal(record.GetValueByKey("progressPercent")),
                    CurrentLayer = ToInt(record.GetValueByKey("currentLayer")),
                    TotalLayers = ToInt(record.GetValueByKey("totalLayers")),

                    NozzleTempC = ToDecimal(record.GetValueByKey("nozzleTempC")),
                    BedTempC = ToDecimal(record.GetValueByKey("bedTempC")),
                    ChamberTempC = ToDecimal(record.GetValueByKey("chamberTempC")),

                    PrintSpeedPercent = ToInt(record.GetValueByKey("printSpeedPercent")),
                    FilamentRemainingGrams = ToDecimal(record.GetValueByKey("filamentRemainingGrams")),
                    PowerWatts = ToDecimal(record.GetValueByKey("powerWatts")),
                    VibrationScore = ToDecimal(record.GetValueByKey("vibrationScore")),

                    ErrorCode = record.GetValueByKey("errorCode")?.ToString()
                });
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<PrinterTelemetryPoint>> QueryTaskRangeAsync(
        string deviceId,
        long externalTaskId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        CancellationToken cancellationToken = default)
    {
        var startIso = startUtc.UtcDateTime.ToString("O");
        var endIso = endUtc.UtcDateTime.ToString("O");

        var flux = $"""
        from(bucket: "{_bucket}")
        |> range(start: time(v: "{startIso}"), stop: time(v: "{endIso}"))
        |> filter(fn: (r) => r._measurement == "printer_telemetry")
        |> filter(fn: (r) => r.deviceId == "{deviceId}")
        |> filter(fn: (r) => r.taskId == "{externalTaskId}")
        |> pivot(rowKey:["_time"], columnKey: ["_field"], valueColumn: "_value")
        |> sort(columns: ["_time"])
        """;

        var queryApi = _client.GetQueryApi();
        var tables = await queryApi.QueryAsync(flux, _org, cancellationToken);

        var result = new List<PrinterTelemetryPoint>();

        foreach (var table in tables)
        {
            foreach (var record in table.Records)
            {
                result.Add(new PrinterTelemetryPoint
                {
                    DeviceId = record.GetValueByKey("deviceId")?.ToString() ?? deviceId,
                    ExternalTaskId = long.TryParse(record.GetValueByKey("taskId")?.ToString(), out var taskId) ? taskId : externalTaskId,
                    TimestampUtc = record.GetTimeInDateTime()?.ToUniversalTime() ?? DateTime.UtcNow,
                    PrintStatus = record.GetValueByKey("printStatus")?.ToString() ?? "UNKNOWN",
                    IsSimulated = bool.TryParse(record.GetValueByKey("isSimulated")?.ToString(), out var sim) && sim,

                    ProgressPercent = ToDecimal(record.GetValueByKey("progressPercent")),
                    CurrentLayer = ToInt(record.GetValueByKey("currentLayer")),
                    TotalLayers = ToInt(record.GetValueByKey("totalLayers")),

                    NozzleTempC = ToDecimal(record.GetValueByKey("nozzleTempC")),
                    BedTempC = ToDecimal(record.GetValueByKey("bedTempC")),
                    ChamberTempC = ToDecimal(record.GetValueByKey("chamberTempC")),

                    PrintSpeedPercent = ToInt(record.GetValueByKey("printSpeedPercent")),
                    FilamentRemainingGrams = ToDecimal(record.GetValueByKey("filamentRemainingGrams")),
                    PowerWatts = ToDecimal(record.GetValueByKey("powerWatts")),
                    VibrationScore = ToDecimal(record.GetValueByKey("vibrationScore")),

                    ErrorCode = record.GetValueByKey("errorCode")?.ToString()
                });
            }
        }

        return result;
    }

    private static decimal ToDecimal(object? value)
        => value is null ? 0m : Convert.ToDecimal(value);

    private static int ToInt(object? value)
        => value is null ? 0 : Convert.ToInt32(value);
}