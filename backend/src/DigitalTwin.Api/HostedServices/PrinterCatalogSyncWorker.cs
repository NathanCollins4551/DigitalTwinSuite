using DigitalTwin.Infrastructure.Sync;

namespace DigitalTwin.Api.HostedServices;

public class PrinterCatalogSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrinterCatalogSyncWorker> _logger;

    public PrinterCatalogSyncWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PrinterCatalogSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrinterCatalogSyncWorker started.");

        await RunSyncCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunSyncCycleAsync(stoppingToken);
        }

        _logger.LogInformation("PrinterCatalogSyncWorker stopped.");
    }

    private async Task RunSyncCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using (var bindScope = _scopeFactory.CreateScope())
            {
                var service = bindScope.ServiceProvider.GetRequiredService<PrinterCatalogSyncService>();
                await service.SyncBindAsync(cancellationToken);
            }

            using (var versionScope = _scopeFactory.CreateScope())
            {
                var service = versionScope.ServiceProvider.GetRequiredService<PrinterCatalogSyncService>();
                await service.SyncVersionAsync(cancellationToken);
            }

            using (var taskScope = _scopeFactory.CreateScope())
            {
                var service = taskScope.ServiceProvider.GetRequiredService<PrinterActivitySyncService>();
                await service.SyncTasksAsync(cancellationToken);
            }

            using (var messageScope = _scopeFactory.CreateScope())
            {
                var service = messageScope.ServiceProvider.GetRequiredService<PrinterActivitySyncService>();
                await service.SyncMessagesAsync(cancellationToken);
            }

            _logger.LogInformation("Full printer sync completed successfully at {Time}.", DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Printer catalog sync failed.");
        }
    }
}