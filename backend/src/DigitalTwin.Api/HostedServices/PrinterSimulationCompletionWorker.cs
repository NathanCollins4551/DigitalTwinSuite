using DigitalTwin.Infrastructure.Simulation;

namespace DigitalTwin.Api.HostedServices;

public class PrinterSimulationCompletionWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrinterSimulationCompletionWorker> _logger;

    public PrinterSimulationCompletionWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PrinterSimulationCompletionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrinterSimulationCompletionWorker started.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<PrinterSimulationService>();

                await service.CompleteExpiredSimulatedTasksAsync(stoppingToken);
                await service.ReleaseExpiredSimulationLocksAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Printer simulation completion worker failed.");
            }
        }
    }
}