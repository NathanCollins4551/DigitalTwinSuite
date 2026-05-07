using DigitalTwin.Application.Abstractions.Telemetry;
using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using DigitalTwin.Infrastructure.Telemetry;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace DigitalTwin.Api.HostedServices;

public class PrinterSimulationTelemetryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrinterSimulationTelemetryWorker> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _nextDueMap = new();

    public PrinterSimulationTelemetryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PrinterSimulationTelemetryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrinterSimulationTelemetryWorker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider.GetRequiredService<DigitalTwinDbContext>();
                var writer = scope.ServiceProvider.GetRequiredService<IPrinterTelemetryWriter>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPrinterTelemetryPublisher>();
                var generator = scope.ServiceProvider.GetRequiredService<PrinterTelemetryGenerator>();

                var now = DateTimeOffset.UtcNow;

                var active = await db.PrinterSimulationControls
                    .AsNoTracking()
                    .Where(x => x.IsLocked && x.SimulationState == "RUNNING" && x.ActivePrinterTaskId != null)
                    .Join(
                        db.Printers.AsNoTracking(),
                        sc => sc.PrinterId,
                        p => p.Id,
                        (sc, p) => new { sc, p })
                    .ToListAsync(stoppingToken);

                foreach (var item in active)
                {
                    var deviceId = item.p.DeviceId;

                    if (_nextDueMap.TryGetValue(deviceId, out var nextDue) && nextDue > now)
                        continue;

                    var task = await db.PrinterTasks
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.Id == item.sc.ActivePrinterTaskId &&
                                 x.IsSimulated &&
                                 x.StatusText == "RUNNING" &&
                                 x.EndTimeUtc == null,
                            stoppingToken);

                    if (task is null)
                        continue;

                    var point = generator.Generate(item.p, task, now);

                    await writer.WriteAsync(point, stoppingToken);
                    publisher.Publish(point);

                    _nextDueMap[deviceId] = now.AddSeconds(Random.Shared.Next(2, 6));
                }

                var activeDeviceIds = active.Select(x => x.p.DeviceId).ToHashSet();
                var staleKeys = _nextDueMap.Keys.Where(k => !activeDeviceIds.Contains(k)).ToList();
                foreach (var stale in staleKeys)
                {
                    _nextDueMap.TryRemove(stale, out _);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Printer simulation telemetry worker failed.");
            }
        }
    }
}