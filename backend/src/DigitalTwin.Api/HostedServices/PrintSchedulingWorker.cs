using DigitalTwin.Infrastructure.Scheduling;

namespace DigitalTwin.Api.HostedServices;

public class PrintSchedulingWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrintSchedulingWorker> _logger;

    public PrintSchedulingWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PrintSchedulingWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrintSchedulingWorker is disabled.");
        await Task.CompletedTask;
        // _logger.LogInformation("PrintSchedulingWorker started.");

        // using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

        // while (!stoppingToken.IsCancellationRequested &&
        //        await timer.WaitForNextTickAsync(stoppingToken))
        // {
        //     try
        //     {
        //         using var scope = _scopeFactory.CreateScope();
        //         var scheduler = scope.ServiceProvider.GetRequiredService<PrintSchedulerService>();

        //         await scheduler.RunDispatchCycleAsync(stoppingToken);
        //         await scheduler.RefreshQueueEstimatesAsync(stoppingToken);
        //     }
        //     catch (OperationCanceledException)
        //     {
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Print scheduling worker failed.");
        //     }
        // }

        // _logger.LogInformation("PrintSchedulingWorker stopped.");
    }
}