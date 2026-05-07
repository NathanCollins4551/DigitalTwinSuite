using System.Text.Json;
using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Simulation;

public class PrinterSimulationService
{
    private readonly DigitalTwinDbContext _db;

    public PrinterSimulationService(DigitalTwinDbContext db)
    {
        _db = db;
    }

    private static readonly string[] DesignTitles =
    {
        "Calibration Cube",
        "Hydraulic Valve Cap",
        "Sensor Bracket",
        "Prototype Housing",
        "Custom Gear Mount",
        "Cooling Duct",
        "Drone Frame Clip",
        "Pipe Joint Adapter",
        "Motor Cover Plate",
        "Filament Guide Arm",
        "Turbine Blade",
        "Heatsink Fan Housing",
        "Control Knob",
        "LCD Bezel",
        "Handle Grip",
        "Battery Enclosure",
        "Servo Horn",
        "Cable Management Clip",
        "Spring Tensioner",
        "Bearing Housing",
        "Pulley Wheel",
        "Mounting Plate",
        "Chassis Rail",
        "Sensor Shield",
        "Gasket Seal Test"
    };

    private static readonly string[] DesignTitlesTranslated =
    {
        "Calibration Cube",
        "Hydraulic Valve Cap",
        "Sensor Support Bracket",
        "Prototype Enclosure",
        "Custom Gear Mount",
        "Cooling Air Duct",
        "Drone Frame Clip",
        "Pipe Joint Adapter",
        "Motor Cover Plate",
        "Filament Guide Arm",
        "Turbine Blade Assembly",
        "Heatsink Fan Housing",
        "User Interface Control Knob",
        "LCD Screen Bezel",
        "Ergonomic Handle Grip",
        "Lithium Battery Enclosure",
        "High-Torque Servo Horn",
        "Cable Management Clip",
        "Spring Tensioner Link",
        "Precision Bearing Housing",
        "Timing Belt Pulley Wheel",
        "Universal Mounting Plate",
        "Chassis Structural Rail",
        "Environmental Sensor Shield",
        "Flexible Gasket Seal Test"
    };

    private static readonly string[] BedTypes =
    {
        "Cool Plate",
        "Engineering Plate",
        "Textured PEI Plate",
        "High Temp Plate"
    };

    private static readonly string[] Modes =
    {
        "Standard",
        "Silent",
        "Sport",
        "Ludicrous"
    };

    private static readonly string[] FilamentTypes =
    {
        "PLA",
        "PETG",
        "ABS",
        "TPU",
        "PA-CF"
    };

    private static readonly string[] FilamentColors =
    {
        "#FFFFFF",
        "#000000",
        "#FF0000",
        "#00FF00",
        "#0000FF",
        "#FFFF00",
        "#FFA500",
        "#808080",
        "#8A2BE2",
        "#00CED1"
    };

    private static string GenerateExternalTaskId()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
    }

    private static long GenerateExternalMessageId()
    {
        var baseId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var noise = Random.Shared.Next(100, 999);
        return long.Parse($"{baseId}{noise}");
    }

    private static (string Title, string TitleTranslated) GetRandomDesignTitle()
    {
        var index = Random.Shared.Next(DesignTitles.Length);
        return (DesignTitles[index], DesignTitlesTranslated[index]);
    }

    private static string GetRandomBedType()
    {
        return BedTypes[Random.Shared.Next(BedTypes.Length)];
    }

    private static string GetRandomMode()
    {
        return Modes[Random.Shared.Next(Modes.Length)];
    }

    private static string GetRandomFilamentType()
    {
        return FilamentTypes[Random.Shared.Next(FilamentTypes.Length)];
    }

    private static string GetRandomColor()
    {
        return FilamentColors[Random.Shared.Next(FilamentColors.Length)];
    }

    private static decimal GetRandomWeight(decimal min, decimal max)
    {
        var value = min + (decimal)Random.Shared.NextDouble() * (max - min);
        return Math.Round(value, 2);
    }

    private static int GetRandomLengthMm()
    {
        return Random.Shared.Next(5000, 45001);
    }

    private static int GetRandomFailedType()
    {
        var failedTypes = new[] { 1, 2, 3, 4, 5 };
        return failedTypes[Random.Shared.Next(failedTypes.Length)];
    }

    private List<PrinterTaskAmsDetail> BuildSimulatedAmsDetails(Guid printerTaskId, DateTimeOffset now)
    {
        var count = Random.Shared.Next(1, 5);
        var result = new List<PrinterTaskAmsDetail>();

        var usedSlots = new HashSet<(int ams, int slot)>();

        for (var i = 0; i < count; i++)
        {
            int ams;
            int slot;

            do
            {
                ams = Random.Shared.Next(0, 2);      // simulate AMS bank 0-1
                slot = Random.Shared.Next(0, 4);     // slot 0-3
            }
            while (!usedSlots.Add((ams, slot)));

            var filamentType = GetRandomFilamentType();
            var sourceColor = GetRandomColor();
            var targetColor = GetRandomColor();

            result.Add(new PrinterTaskAmsDetail
            {
                Id = Guid.NewGuid(),
                PrinterTaskId = printerTaskId,
                Ams = ams,
                AmsId = ams,
                SlotId = slot,
                NozzleId = 0,
                FilamentId = $"{filamentType}-{slot}-{Random.Shared.Next(1000, 9999)}",
                FilamentType = filamentType,
                TargetFilamentType = filamentType,
                SourceColor = sourceColor,
                TargetColor = targetColor,
                WeightGrams = GetRandomWeight(5m, 80m),
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        return result;
    }


    ///////////////////////////////////////////////////////////////////////

    public async Task<StartSimulationResult> StartPrinterAsync(
        string deviceId,
        string? designTitle,
        int simulatedDurationSeconds,
        CancellationToken cancellationToken = default)
    {
        var printer = await _db.Printers
            .Include(x => x.SimulationControl)
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (printer is null)
            return new StartSimulationResult
                {
                    Success = false,
                    Message = "Printer not found."
                };

        if (!printer.IsOnline)
            return new StartSimulationResult
                {
                    Success = false,
                    Message = "Printer is offline. Start is not allowed."
                };

        var allowedStatuses = new[] { "ACTIVE", "SUCCESS", "FAIL" };
        if (string.IsNullOrWhiteSpace(printer.PrintStatus) || !allowedStatuses.Contains(printer.PrintStatus))
            return new StartSimulationResult
                {
                    Success = false,
                    Message = $"Start not allowed when PrintStatus is '{printer.PrintStatus}'."
                };

        var now = DateTimeOffset.UtcNow;

        var runningTask = await _db.PrinterTasks
            .FirstOrDefaultAsync(
                x => x.DeviceId == deviceId &&
                    x.IsSimulated &&
                    x.EndTimeUtc == null &&
                    x.StatusText == "RUNNING",
                cancellationToken);

        if (runningTask is not null)
            return new StartSimulationResult
                {
                    Success = false,
                    Message = "This printer already has a running simulated task."
                };

        var titles = GetRandomDesignTitle();
        var externalTaskIdText = GenerateExternalTaskId();
        var externalTaskId = long.Parse(externalTaskIdText);

        var finalDesignTitle = string.IsNullOrWhiteSpace(designTitle)
            ? titles.Title
            : designTitle;

        var task = new PrinterTask
        {
            Id = Guid.NewGuid(),
            ExternalTaskId = externalTaskId,
            TaskAlias = PrinterTask.BuildTaskAlias(printer.Name, printer.DeviceId, externalTaskId),
            PrinterId = printer.Id,
            DeviceId = printer.DeviceId,
            DeviceName = printer.Name,
            DeviceModel = printer.ProductName ?? printer.ModelName,
            DesignTitle = finalDesignTitle,
            DesignTitleTranslated = titles.TitleTranslated,
            StatusText = "RUNNING",
            FailedType = null,
            Mode = GetRandomMode(),
            BedType = GetRandomBedType(),
            CostTimeSeconds = simulatedDurationSeconds,
            LengthMm = GetRandomLengthMm(),
            WeightGrams = GetRandomWeight(20m, 250m),
            StartTimeUtc = now,
            EndTimeUtc = null,
            CoverUrl = $"https://sim.local/covers/{externalTaskId}.jpg",
            IsSimulated = true,
            SimulatedCompleteAtUtc = now.AddSeconds(simulatedDurationSeconds),
            RawJson = JsonSerializer.Serialize(new
            {
                source = "simulation",
                action = "start",
                deviceId = printer.DeviceId,
                deviceName = printer.Name,
                deviceModel = printer.ProductName ?? printer.ModelName,
                taskId = externalTaskId,
                designTitle = finalDesignTitle,
                designTitleTranslated = titles.TitleTranslated,
                mode = "simulated",
                durationSeconds = simulatedDurationSeconds
            }),
            SourceUpdatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.PrinterTasks.Add(task);
        await _db.SaveChangesAsync(cancellationToken);

        var amsDetails = BuildSimulatedAmsDetails(task.Id, now);
        if (amsDetails.Count > 0)
        {
            _db.PrinterTaskAmsDetails.AddRange(amsDetails);
            await _db.SaveChangesAsync(cancellationToken);
        }

        var control = printer.SimulationControl;
        if (control is null)
        {
            control = new PrinterSimulationControl
            {
                Id = Guid.NewGuid(),
                PrinterId = printer.Id,
                CreatedAtUtc = now
            };
            _db.PrinterSimulationControls.Add(control);
        }

        control.IsLocked = true;
        control.LockedUntilUtc = task.SimulatedCompleteAtUtc?.AddMinutes(1);
        control.SimulationState = "RUNNING";
        control.ActivePrinterTaskId = task.Id;
        control.UpdatedAtUtc = now;

        printer.PrintStatus = "RUNNING";
        printer.UpdatedAtUtc = now;

        var totalAmsWeight = amsDetails.Sum(x => x.WeightGrams ?? 0m);

        var message = new PrinterMessage
        {
            Id = Guid.NewGuid(),
            ExternalMessageId = GenerateExternalMessageId(),
            PrinterId = printer.Id,
            RelatedPrinterTaskId = task.Id,
            ExternalTaskId = task.ExternalTaskId,
            Type = 1,
            IsRead = 0,
            CreateTimeUtc = now,
            DeviceId = printer.DeviceId,
            DeviceName = printer.Name,
            TaskStatus = 1,
            Title = "Simulated print started",
            Detail = $"Started simulated task '{task.DesignTitle}' on {printer.Name}. Mode={task.Mode}, BedType={task.BedType}, EstimatedDuration={simulatedDurationSeconds}s, EstimatedWeight={task.WeightGrams}g, AmsMaterials={amsDetails.Count}, AmsWeight={totalAmsWeight}g.",
            CoverUrl = task.CoverUrl,
            DesignId = Random.Shared.Next(1000, 99999),
            DesignTitle = task.DesignTitle,
            RawJson = JsonSerializer.Serialize(new
            {
                source = "simulation",
                action = "start_message",
                taskId = task.ExternalTaskId,
                printerId = printer.Id,
                deviceId = printer.DeviceId,
                designTitle = task.DesignTitle,
                estimatedDuration = simulatedDurationSeconds,
                estimatedWeight = task.WeightGrams,
                amsDetailCount = amsDetails.Count
            }),
            SourceUpdatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.PrinterMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return new StartSimulationResult
        {
            Success = true,
            Message = "Simulated start successful.",
            PrinterTaskId = task.Id,
            ExternalTaskId = task.ExternalTaskId
        };
    }


    ///////////////////////////////////////////////////////////////////////

    public async Task<(bool Success, string Message)> StopPrinterAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        var printer = await _db.Printers
            .Include(x => x.SimulationControl)
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, cancellationToken);

        if (printer is null)
            return (false, "Printer not found.");

        if (!printer.IsOnline)
            return (false, "Printer is offline. Stop is not allowed.");

        var control = printer.SimulationControl;
        if (control is null || !control.IsLocked)
            return (false, "This printer is not currently simulation-controlled.");

        if (!string.Equals(control.SimulationState, "RUNNING", StringComparison.OrdinalIgnoreCase))
            return (false, $"Stop not allowed when simulation state is '{control.SimulationState}'.");

        if (!string.Equals(printer.PrintStatus, "RUNNING", StringComparison.OrdinalIgnoreCase))
            return (false, $"Stop not allowed when PrintStatus is '{printer.PrintStatus}'.");

        var runningTask = await _db.PrinterTasks
            .FirstOrDefaultAsync(
                x => x.Id == control.ActivePrinterTaskId &&
                    x.IsSimulated &&
                    x.EndTimeUtc == null &&
                    x.StatusText == "RUNNING",
                cancellationToken);

        if (runningTask is null)
            return (false, "No running simulated task found for this printer.");

        var now = DateTimeOffset.UtcNow;
        var elapsedSeconds = runningTask.StartTimeUtc.HasValue
            ? (int)Math.Max(1, (now - runningTask.StartTimeUtc.Value).TotalSeconds)
            : 1;

        printer.PrintStatus = "FAIL";
        printer.UpdatedAtUtc = now;

        runningTask.StatusText = "FAIL";
        runningTask.FailedType = GetRandomFailedType();
        runningTask.EndTimeUtc = now;
        runningTask.CostTimeSeconds = elapsedSeconds;
        runningTask.UpdatedAtUtc = now;
        runningTask.SourceUpdatedAtUtc = now;

        control.IsLocked = true;
        control.LockedUntilUtc = now.AddMinutes(2);
        control.SimulationState = "FAIL";
        control.ActivePrinterTaskId = null;
        control.UpdatedAtUtc = now;

        var amsDetails = await _db.PrinterTaskAmsDetails
            .Where(x => x.PrinterTaskId == runningTask.Id)
            .ToListAsync(cancellationToken);

        var consumedWeight = amsDetails.Sum(x => x.WeightGrams ?? 0m);

        var message = new PrinterMessage
        {
            Id = Guid.NewGuid(),
            ExternalMessageId = GenerateExternalMessageId(),
            PrinterId = printer.Id,
            RelatedPrinterTaskId = runningTask.Id,
            ExternalTaskId = runningTask.ExternalTaskId,
            Type = 2,
            IsRead = 0,
            CreateTimeUtc = now,
            DeviceId = printer.DeviceId,
            DeviceName = printer.Name,
            TaskStatus = 2,
            Title = "Simulated print stopped",
            Detail = $"Simulated task '{runningTask.DesignTitle}' was manually stopped after {elapsedSeconds}s and marked as FAIL. FailedType={runningTask.FailedType}, MaterialTracked={amsDetails.Count}, EstimatedConsumedWeight={consumedWeight}g.",
            CoverUrl = runningTask.CoverUrl,
            DesignId = Random.Shared.Next(1000, 99999),
            DesignTitle = runningTask.DesignTitle,
            RawJson = JsonSerializer.Serialize(new
            {
                source = "simulation",
                action = "stop_message",
                taskId = runningTask.ExternalTaskId,
                deviceId = printer.DeviceId,
                elapsedSeconds,
                failedType = runningTask.FailedType
            }),
            SourceUpdatedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _db.PrinterMessages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return (true, "Simulated stop successful.");
    }


    ////////////////////////////////////////////////////////////////////////

    public async Task CompleteExpiredSimulatedTasksAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var expiredTasks = await _db.PrinterTasks
            .Where(x =>
                x.IsSimulated &&
                x.StatusText == "RUNNING" &&
                x.EndTimeUtc == null &&
                x.SimulatedCompleteAtUtc != null &&
                x.SimulatedCompleteAtUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var task in expiredTasks)
        {
            var printer = await _db.Printers
                .Include(x => x.SimulationControl)
                .FirstOrDefaultAsync(x => x.DeviceId == task.DeviceId, cancellationToken);

            var elapsedSeconds = task.StartTimeUtc.HasValue
                ? (int)Math.Max(1, (now - task.StartTimeUtc.Value).TotalSeconds)
                : (task.CostTimeSeconds ?? 0);

            var amsDetails = await _db.PrinterTaskAmsDetails
                .Where(x => x.PrinterTaskId == task.Id)
                .ToListAsync(cancellationToken);

            var totalWeight = amsDetails.Sum(x => x.WeightGrams ?? 0m);

            if (printer is not null)
            {
                printer.PrintStatus = "SUCCESS";
                printer.UpdatedAtUtc = now;

                if (printer.SimulationControl is not null)
                {
                    printer.SimulationControl.IsLocked = true;
                    printer.SimulationControl.LockedUntilUtc = now.AddMinutes(2);
                    printer.SimulationControl.SimulationState = "SUCCESS";
                    printer.SimulationControl.ActivePrinterTaskId = null;
                    printer.SimulationControl.UpdatedAtUtc = now;
                }
            }

            task.StatusText = "SUCCESS";
            task.EndTimeUtc = now;
            task.CostTimeSeconds = elapsedSeconds;
            task.UpdatedAtUtc = now;
            task.SourceUpdatedAtUtc = now;

            var message = new PrinterMessage
            {
                Id = Guid.NewGuid(),
                ExternalMessageId = GenerateExternalMessageId(),
                PrinterId = printer?.Id,
                RelatedPrinterTaskId = task.Id,
                ExternalTaskId = task.ExternalTaskId,
                Type = 3,
                IsRead = 0,
                CreateTimeUtc = now,
                DeviceId = task.DeviceId,
                DeviceName = task.DeviceName,
                TaskStatus = 3,
                Title = "Simulated print completed",
                Detail = $"Simulated task '{task.DesignTitle}' completed successfully in {elapsedSeconds}s. MaterialTracked={amsDetails.Count}, TotalWeight={totalWeight}g, BedType={task.BedType}, Mode={task.Mode}.",
                CoverUrl = task.CoverUrl,
                DesignId = Random.Shared.Next(1000, 99999),
                DesignTitle = task.DesignTitle,
                RawJson = JsonSerializer.Serialize(new
                {
                    source = "simulation",
                    action = "complete_message",
                    taskId = task.ExternalTaskId,
                    deviceId = task.DeviceId,
                    elapsedSeconds,
                    totalWeight
                }),
                SourceUpdatedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            _db.PrinterMessages.Add(message);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReleaseExpiredSimulationLocksAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var controls = await _db.PrinterSimulationControls
            .Where(x => x.IsLocked && x.LockedUntilUtc != null && x.LockedUntilUtc <= now)
            .ToListAsync(cancellationToken);

        foreach (var control in controls)
        {
            control.IsLocked = false;
            control.LockedUntilUtc = null;
            control.SimulationState = null;
            control.ActivePrinterTaskId = null;
            control.UpdatedAtUtc = now;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
