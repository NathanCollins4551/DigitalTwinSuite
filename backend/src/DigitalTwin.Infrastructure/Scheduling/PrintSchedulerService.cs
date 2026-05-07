using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using DigitalTwin.Infrastructure.Simulation;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Scheduling;

public class PrintSchedulerService
{
    private readonly DigitalTwinDbContext _db;
    private readonly PrinterSimulationService _simulationService;

    public PrintSchedulerService(
        DigitalTwinDbContext db,
        PrinterSimulationService simulationService)
    {
        _db = db;
        _simulationService = simulationService;
    }

    // public async Task RunDispatchCycleAsync(CancellationToken cancellationToken = default)
    // {
    //     var now = DateTimeOffset.UtcNow;

    //     await ReconcileRunningJobsAsync(cancellationToken);

    //     var control = await GetOrCreateSchedulerControlAsync(cancellationToken);
    //     if (control.IsPaused)
    //         return;

    //     var queuedJobs = await _db.ScheduledPrintJobs
    //         .Where(x => x.Status == "QUEUED")
    //         .Where(x => !x.RequestedStartAfterUtc.HasValue || x.RequestedStartAfterUtc <= now)
    //         .OrderByDescending(x => x.Priority)
    //         .ThenBy(x => x.DueAtUtc)
    //         .ThenBy(x => x.CreatedAtUtc)
    //         .ToListAsync(cancellationToken);

    //     if (queuedJobs.Count == 0)
    //         return;

    //     var availablePrinters = await GetAvailablePrintersAsync(cancellationToken);
    //     if (availablePrinters.Count == 0)
    //         return;

    //     foreach (var job in queuedJobs)
    //     {
    //         var selection = SelectPrinterForJob(job, availablePrinters);
    //         if (selection is null)
    //             continue;

    //         var printer = selection.Value.Printer;
    //         var duration = job.EstimatedDurationSeconds ?? 300;

    //         var result = await _simulationService.StartPrinterAsync(
    //             printer.DeviceId,
    //             job.DisplayName,
    //             duration,
    //             cancellationToken);

    //         if (!result.Success)
    //             continue;

    //         var startAt = DateTimeOffset.UtcNow;
    //         var finishAt = startAt.AddSeconds(duration);

    //         job.Status = "RUNNING";
    //         job.AssignedPrinterId = printer.Id;
    //         job.StartedPrinterTaskId = result.PrinterTaskId;
    //         job.SchedulerDecisionReason = selection.Value.Reason;
    //         job.CompatibilityNote = selection.Value.CompatibilityNote;
    //         job.EstimatedStartAtUtc = startAt;
    //         job.EstimatedFinishAtUtc = finishAt;
    //         job.UpdatedAtUtc = startAt;

    //         availablePrinters.RemoveAll(x => x.Id == printer.Id);

    //         if (availablePrinters.Count == 0)
    //             break;
    //     }

    //     await _db.SaveChangesAsync(cancellationToken);
    // }

    // public async Task ReconcileRunningJobsAsync(CancellationToken cancellationToken = default)
    // {
    //     var runningJobs = await _db.ScheduledPrintJobs
    //         .Where(x => x.Status == "RUNNING" && x.StartedPrinterTaskId != null)
    //         .ToListAsync(cancellationToken);

    //     if (runningJobs.Count == 0)
    //         return;

    //     foreach (var job in runningJobs)
    //     {
    //         var task = await _db.PrinterTasks
    //             .AsNoTracking()
    //             .FirstOrDefaultAsync(x => x.Id == job.StartedPrinterTaskId, cancellationToken);

    //         if (task is null)
    //             continue;

    //         if (task.StatusText == "SUCCESS")
    //         {
    //             job.Status = "COMPLETED";
    //             job.UpdatedAtUtc = DateTimeOffset.UtcNow;
    //         }
    //         else if (task.StatusText == "FAIL")
    //         {
    //             job.Status = "FAILED";
    //             job.UpdatedAtUtc = DateTimeOffset.UtcNow;
    //         }
    //     }

    //     await _db.SaveChangesAsync(cancellationToken);
    // }

    // public async Task RefreshQueueEstimatesAsync(CancellationToken cancellationToken = default)
    // {
    //     var now = DateTimeOffset.UtcNow;

    //     var queuedJobs = await _db.ScheduledPrintJobs
    //         .Where(x => x.Status == "QUEUED")
    //         .OrderByDescending(x => x.Priority)
    //         .ThenBy(x => x.DueAtUtc)
    //         .ThenBy(x => x.CreatedAtUtc)
    //         .ToListAsync(cancellationToken);

    //     var availablePrinters = await GetAvailablePrintersAsync(cancellationToken);

    //     var runningTasksByPrinter = await _db.PrinterTasks
    //         .AsNoTracking()
    //         .Where(x => x.IsSimulated && x.StatusText == "RUNNING" && x.EndTimeUtc == null && x.PrinterId != null)
    //         .ToListAsync(cancellationToken);

    //     var printerAvailableAt = new Dictionary<Guid, DateTimeOffset>();

    //     foreach (var printer in availablePrinters)
    //     {
    //         printerAvailableAt[printer.Id] = now;
    //     }

    //     foreach (var runningTask in runningTasksByPrinter)
    //     {
    //         if (!runningTask.PrinterId.HasValue)
    //             continue;

    //         var finish = runningTask.SimulatedCompleteAtUtc ?? now.AddMinutes(5);
    //         printerAvailableAt[runningTask.PrinterId.Value] = finish;
    //     }

    //     foreach (var job in queuedJobs)
    //     {
    //         var selection = SelectPrinterForJob(job, availablePrinters);
    //         if (selection is null)
    //         {
    //             job.CompatibilityNote = "No currently available printer matched this job.";
    //             job.EstimatedStartAtUtc = null;
    //             job.EstimatedFinishAtUtc = null;
    //             job.UpdatedAtUtc = now;
    //             continue;
    //         }

    //         var printer = selection.Value.Printer;

    //         var startAt = printerAvailableAt.TryGetValue(printer.Id, out var availableAt)
    //             ? availableAt
    //             : now;

    //         var duration = job.EstimatedDurationSeconds ?? 300;
    //         var finishAt = startAt.AddSeconds(duration);

    //         job.CompatibilityNote = selection.Value.CompatibilityNote;
    //         job.EstimatedStartAtUtc = startAt;
    //         job.EstimatedFinishAtUtc = finishAt;
    //         job.UpdatedAtUtc = now;

    //         printerAvailableAt[printer.Id] = finishAt;
    //     }

    //     await _db.SaveChangesAsync(cancellationToken);
    // }

    // private async Task<List<Printer>> GetAvailablePrintersAsync(CancellationToken cancellationToken)
    // {
    //     var printers = await _db.Printers
    //         .AsNoTracking()
    //         .Include(x => x.SimulationControl)
    //         .Include(x => x.AmsUnits)
    //         .Where(x => x.IsOnline)
    //         .ToListAsync(cancellationToken);

    //     var result = new List<Printer>();

    //     foreach (var printer in printers)
    //     {
    //         if (printer.SimulationControl is not null &&
    //             printer.SimulationControl.IsLocked &&
    //             printer.SimulationControl.LockedUntilUtc.HasValue &&
    //             printer.SimulationControl.LockedUntilUtc > DateTimeOffset.UtcNow &&
    //             printer.SimulationControl.SimulationState == "RUNNING")
    //         {
    //             continue;
    //         }

    //         var hasRunningTask = await _db.PrinterTasks
    //             .AsNoTracking()
    //             .AnyAsync(
    //                 x => x.DeviceId == printer.DeviceId &&
    //                      x.IsSimulated &&
    //                      x.EndTimeUtc == null &&
    //                      x.StatusText == "RUNNING",
    //                 cancellationToken);

    //         if (hasRunningTask)
    //             continue;

    //         result.Add(printer);
    //     }

    //     return result.OrderBy(x => x.Name).ToList();
    // }

    // private static (Printer Printer, string Reason, string CompatibilityNote)? SelectPrinterForJob(
    //     ScheduledPrintJob job,
    //     List<Printer> availablePrinters)
    // {
    //     if (availablePrinters.Count == 0)
    //         return null;

    //     if (!job.AllowAnyPrinter)
    //     {
    //         if (!job.PreferredPrinterId.HasValue)
    //             return null;

    //         var preferred = availablePrinters.FirstOrDefault(x => x.Id == job.PreferredPrinterId.Value);
    //         if (preferred is null)
    //             return null;

    //         return (
    //             preferred,
    //             $"Selected preferred printer '{preferred.Name}' because this job requires a specific printer.",
    //             BuildCompatibilityNote(job, preferred)
    //         );
    //     }

    //     var scored = availablePrinters
    //         .Select(printer => new
    //         {
    //             Printer = printer,
    //             Score = ScorePrinterForJob(job, printer)
    //         })
    //         .OrderByDescending(x => x.Score)
    //         .ThenBy(x => x.Printer.Name)
    //         .ToList();

    //     var winner = scored.First().Printer;

    //     string reason;
    //     if (job.PreferredPrinterId.HasValue && winner.Id == job.PreferredPrinterId.Value)
    //     {
    //         reason = $"Selected preferred printer '{winner.Name}' because it is available and has the best score.";
    //     }
    //     else
    //     {
    //         reason = $"Selected printer '{winner.Name}' because it was the best available match for this queued job.";
    //     }

    //     return (
    //         winner,
    //         reason,
    //         BuildCompatibilityNote(job, winner)
    //     );
    // }

    // private static int ScorePrinterForJob(ScheduledPrintJob job, Printer printer)
    // {
    //     var score = 0;

    //     if (job.PreferredPrinterId.HasValue && printer.Id == job.PreferredPrinterId.Value)
    //         score += 1000;

    //     if (printer.AmsUnits is { Count: > 0 })
    //         score += 10;

    //     if (!string.IsNullOrWhiteSpace(job.RequestedMaterialType) && printer.AmsUnits is { Count: > 0 })
    //         score += 20;

    //     if (!string.IsNullOrWhiteSpace(job.RequestedColor) && printer.AmsUnits is { Count: > 0 })
    //         score += 10;

    //     return score;
    // }

    // private static string BuildCompatibilityNote(ScheduledPrintJob job, Printer printer)
    // {
    //     var notes = new List<string>();

    //     if (job.PreferredPrinterId.HasValue && printer.Id == job.PreferredPrinterId.Value)
    //         notes.Add("Preferred printer matched.");

    //     if (printer.AmsUnits.Count > 0)
    //         notes.Add($"Printer has {printer.AmsUnits.Count} AMS unit(s).");
    //     else
    //         notes.Add("Printer has no AMS units tracked.");

    //     if (!string.IsNullOrWhiteSpace(job.RequestedMaterialType))
    //         notes.Add($"Requested material: {job.RequestedMaterialType}.");

    //     if (!string.IsNullOrWhiteSpace(job.RequestedColor))
    //         notes.Add($"Requested color: {job.RequestedColor}.");

    //     return string.Join(" ", notes);
    // }

    // private async Task<SchedulerControl> GetOrCreateSchedulerControlAsync(CancellationToken cancellationToken)
    // {
    //     var control = await _db.SchedulerControls.FirstOrDefaultAsync(cancellationToken);

    //     if (control is not null)
    //         return control;

    //     control = new SchedulerControl
    //     {
    //         Id = Guid.NewGuid(),
    //         IsPaused = false,
    //         PauseReason = null,
    //         CreatedAtUtc = DateTimeOffset.UtcNow,
    //         UpdatedAtUtc = DateTimeOffset.UtcNow
    //     };

    //     _db.SchedulerControls.Add(control);
    //     await _db.SaveChangesAsync(cancellationToken);

    //     return control;
    // }
}