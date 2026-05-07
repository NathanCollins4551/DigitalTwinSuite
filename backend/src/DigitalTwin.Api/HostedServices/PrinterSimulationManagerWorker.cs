using DigitalTwin.Infrastructure.Persistence;
using DigitalTwin.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Api.HostedServices;

public class PrinterSimulationManagerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrinterSimulationManagerWorker> _logger;
    private readonly IConfiguration _configuration;

    public PrinterSimulationManagerWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<PrinterSimulationManagerWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue<bool>("Workers:EnablePrinterSimulationManagerWorker", false))
        {
            _logger.LogInformation("PrinterSimulationManagerWorker is disabled.");
            return;
        }

        _logger.LogInformation("PrinterSimulationManagerWorker started.");

        // Check every minute
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunManagementCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Printer simulation manager worker failed during cycle.");
            }
        }
    }

    private async Task RunManagementCycleAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DigitalTwinDbContext>();
        var simulationService = scope.ServiceProvider.GetRequiredService<PrinterSimulationService>();

        // 1. Get all online printers and their current simulation state
        var printers = await db.Printers
            .AsNoTracking()
            .Include(x => x.SimulationControl)
            .Where(x => x.IsOnline)
            .ToListAsync(stoppingToken);

        if (!printers.Any())
        {
            _logger.LogWarning("No online printers found to manage.");
            return;
        }

        var totalOnline = printers.Count;
        var runningCount = printers.Count(p => 
            p.SimulationControl != null && 
            p.SimulationControl.IsLocked && 
            p.SimulationControl.SimulationState == "RUNNING");

        // Use the user's requested tolerance: 55% - 65%
        var minTarget = (int)Math.Floor(totalOnline * 0.55);
        var maxTarget = (int)Math.Ceiling(totalOnline * 0.65);
        var idealTarget = (int)Math.Round(totalOnline * 0.60);

        _logger.LogInformation("Simulation Stats: {Running}/{Total} online printers. Target Range: {Min}-{Max} (Ideal: {Ideal})", 
            runningCount, totalOnline, minTarget, maxTarget, idealTarget);

        if (runningCount < minTarget)
        {
            var needed = idealTarget - runningCount;
            _logger.LogInformation("Below minimum tolerance. Attempting to start {Count} new simulations.", needed);

            // Find idle printers: Online, not locked, and in an allowable status for starting
            var allowedStatuses = new[] { "ACTIVE", "SUCCESS", "FAIL" };
            var idlePrinters = printers
                .Where(p => (p.SimulationControl == null || !p.SimulationControl.IsLocked) && 
                            allowedStatuses.Contains(p.PrintStatus ?? "ACTIVE"))
                .OrderBy(_ => Guid.NewGuid()) // Shuffle to pick random printers
                .Take(needed)
                .ToList();

            foreach (var printer in idlePrinters)
            {
                // Randomize duration: 1 hour to 8 hours
                var durationSeconds = Random.Shared.Next(3600, 28801);
                
                var result = await simulationService.StartPrinterAsync(
                    printer.DeviceId, 
                    null, // Use random design title
                    durationSeconds, 
                    stoppingToken);

                if (result.Success)
                {
                    _logger.LogInformation("Started simulation on {PrinterName} ({DeviceId}) for {Duration}s.", 
                        printer.Name, printer.DeviceId, durationSeconds);
                }
                else
                {
                    _logger.LogWarning("Failed to start simulation on {PrinterName}: {Message}", 
                        printer.Name, result.Message);
                }

                // Small delay to stagger starts slightly
                await Task.Delay(200, stoppingToken);
            }
        }
        else if (runningCount > maxTarget)
        {
            _logger.LogInformation("Above maximum tolerance. Letting simulations finish naturally.");
        }
        else
        {
            _logger.LogInformation("Within healthy tolerance.");
        }
    }
}
