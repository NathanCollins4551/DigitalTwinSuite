using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DigitalTwin.Api.Seeds;

public static class PrinterSeeder
{
    public static async Task SeedEverythingAsync(DigitalTwinDbContext context, string rootPath)
    {
        await SeedPrintersAsync(context, Path.Combine(rootPath, "gannon_printers.json"));
        await SeedTasksAsync(context, Path.Combine(rootPath, "gannon_tasks.json"));
        await SeedMessagesAsync(context, Path.Combine(rootPath, "gannon_messages.json"));
    }

    public static async Task SeedPrintersAsync(DigitalTwinDbContext context, string jsonFilePath)
    {
        Console.WriteLine($"[PrinterSeeder] Starting printer seed. Checking file: {jsonFilePath}");

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"[PrinterSeeder] ERROR: Printer JSON file not found at {jsonFilePath}");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var printerDtos = JsonSerializer.Deserialize<List<PrinterDto>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (printerDtos == null || !printerDtos.Any())
            {
                Console.WriteLine("[PrinterSeeder] WARNING: No printers found in JSON file");
                return;
            }

            var existingPrinters = await context.Printers.Include(p => p.AmsUnits).ToDictionaryAsync(x => x.DeviceId);
            var newPrinters = new List<Printer>();
            var amsUnitsToAdd = new List<PrinterAmsUnit>();

            foreach (var dto in printerDtos)
            {
                if (existingPrinters.TryGetValue(dto.DeviceId, out var existing))
                {
                    // If printer exists but has no AMS units and should have them, add them
                    if (dto.AmsUnitCount > 0 && !existing.AmsUnits.Any())
                    {
                        for (int i = 0; i < dto.AmsUnitCount; i++)
                        {
                            amsUnitsToAdd.Add(new PrinterAmsUnit
                            {
                                Id = Guid.NewGuid(),
                                PrinterId = existing.Id,
                                AmsIndex = i,
                                CreatedAtUtc = DateTime.UtcNow,
                                UpdatedAtUtc = DateTime.UtcNow,
                                LastSeenAtUtc = DateTime.UtcNow
                            });
                        }
                    }
                    continue;
                }

                var p = new Printer
                {
                    Id = Guid.NewGuid(),
                    DeviceId = dto.DeviceId,
                    Name = dto.Name,
                    IsOnline = dto.IsOnline,
                    PrintStatus = dto.PrintStatus,
                    ModelName = dto.ModelName,
                    ProductName = dto.ProductName,
                    Structure = dto.Structure,
                    NozzleDiameterMm = dto.NozzleDiameterMm,
                    IsAmsSupported = dto.AmsUnitCount > 0,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                if (dto.AmsUnitCount > 0)
                {
                    for (int i = 0; i < dto.AmsUnitCount; i++)
                    {
                        p.AmsUnits.Add(new PrinterAmsUnit
                        {
                            Id = Guid.NewGuid(),
                            AmsIndex = i,
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow,
                            LastSeenAtUtc = DateTime.UtcNow
                        });
                    }
                }
                newPrinters.Add(p);
            }

            if (newPrinters.Any()) await context.Printers.AddRangeAsync(newPrinters);
            if (amsUnitsToAdd.Any()) await context.PrinterAmsUnits.AddRangeAsync(amsUnitsToAdd);
            
            if (newPrinters.Any() || amsUnitsToAdd.Any())
            {
                await context.SaveChangesAsync();
                Console.WriteLine($"[PrinterSeeder] SUCCESS: Seeded {newPrinters.Count} new printers and {amsUnitsToAdd.Count} AMS units for existing printers.");
            }
            else
            {
                Console.WriteLine("[PrinterSeeder] No new printers or AMS units to seed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrinterSeeder] ERROR: Exception during printer seeding: {ex.Message}");
        }
    }

    public static async Task SeedTasksAsync(DigitalTwinDbContext context, string jsonFilePath)
    {
        Console.WriteLine($"[PrinterSeeder] Starting task seed. Checking file: {jsonFilePath}");

        if (await context.PrinterTasks.AnyAsync())
        {
            Console.WriteLine("[PrinterSeeder] Tasks already exist. Skipping task seed.");
            return;
        }

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"[PrinterSeeder] ERROR: Task JSON file not found at {jsonFilePath}");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var taskDtos = JsonSerializer.Deserialize<List<TaskDto>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (taskDtos == null || !taskDtos.Any()) return;

            var printers = await context.Printers.ToDictionaryAsync(x => x.DeviceId, x => x.Id);
            var tasks = new List<PrinterTask>();

            foreach (var dto in taskDtos)
            {
                if (!printers.TryGetValue(dto.DeviceId, out var printerId)) continue;

                var task = new PrinterTask
                {
                    Id = Guid.NewGuid(),
                    ExternalTaskId = dto.ExternalTaskId,
                    TaskAlias = dto.TaskAlias ?? PrinterTask.BuildTaskAlias(dto.DeviceName, dto.DeviceId, dto.ExternalTaskId),
                    PrinterId = printerId,
                    DeviceId = dto.DeviceId,
                    DeviceName = dto.DeviceName,
                    DeviceModel = dto.DeviceModel,
                    DesignTitle = dto.DesignTitle,
                    DesignTitleTranslated = dto.DesignTitleTranslated,
                    StartTimeUtc = dto.StartTimeUtc,
                    EndTimeUtc = dto.EndTimeUtc,
                    CostTimeSeconds = dto.CostTimeSeconds,
                    LengthMm = dto.LengthMm,
                    WeightGrams = dto.WeightGrams,
                    BedType = dto.BedType,
                    Mode = dto.Mode,
                    FailedType = dto.FailedType,
                    CoverUrl = dto.CoverUrl,
                    RawJson = JsonSerializer.Serialize(dto),
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    SourceUpdatedAtUtc = DateTime.UtcNow,
                    StatusText = dto.EndTimeUtc.HasValue ? "COMPLETED" : "RUNNING"
                };

                if (dto.AmsDetails != null)
                {
                    foreach (var amsDto in dto.AmsDetails)
                    {
                        task.AmsDetails.Add(new PrinterTaskAmsDetail
                        {
                            Id = Guid.NewGuid(),
                            Ams = amsDto.Ams,
                            AmsId = amsDto.AmsId,
                            SlotId = amsDto.SlotId,
                            NozzleId = amsDto.NozzleId,
                            FilamentId = amsDto.FilamentId,
                            FilamentType = amsDto.FilamentType,
                            TargetFilamentType = amsDto.TargetFilamentType,
                            SourceColor = amsDto.SourceColor,
                            TargetColor = amsDto.TargetColor,
                            WeightGrams = amsDto.WeightGrams,
                            CreatedAtUtc = DateTime.UtcNow,
                            UpdatedAtUtc = DateTime.UtcNow
                        });
                    }
                }

                tasks.Add(task);
            }

            await context.PrinterTasks.AddRangeAsync(tasks);
            await context.SaveChangesAsync();
            Console.WriteLine($"[PrinterSeeder] SUCCESS: Seeded {tasks.Count} tasks.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrinterSeeder] ERROR: Exception during task seeding: {ex.Message}");
        }
    }

    public static async Task SeedMessagesAsync(DigitalTwinDbContext context, string jsonFilePath)
    {
        Console.WriteLine($"[PrinterSeeder] Starting message seed. Checking file: {jsonFilePath}");

        if (await context.PrinterMessages.AnyAsync())
        {
            Console.WriteLine("[PrinterSeeder] Messages already exist. Skipping message seed.");
            return;
        }

        if (!File.Exists(jsonFilePath))
        {
            Console.WriteLine($"[PrinterSeeder] ERROR: Message JSON file not found at {jsonFilePath}");
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(jsonFilePath);
            var messageDtos = JsonSerializer.Deserialize<List<MessageDto>>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (messageDtos == null || !messageDtos.Any()) return;

            var printers = await context.Printers.ToDictionaryAsync(x => x.DeviceId, x => x.Id);
            var tasks = await context.PrinterTasks.ToDictionaryAsync(x => x.ExternalTaskId, x => x.Id);
            var messages = new List<PrinterMessage>();

            foreach (var dto in messageDtos)
            {
                if (dto.DeviceId == null || !printers.TryGetValue(dto.DeviceId, out var printerId)) continue;

                var msg = new PrinterMessage
                {
                    Id = Guid.NewGuid(),
                    ExternalMessageId = dto.ExternalMessageId,
                    PrinterId = printerId,
                    ExternalTaskId = dto.ExternalTaskId,
                    RelatedPrinterTaskId = dto.ExternalTaskId.HasValue && tasks.TryGetValue(dto.ExternalTaskId.Value, out var taskId) ? taskId : null,
                    Type = dto.Type,
                    IsRead = dto.IsRead,
                    CreateTimeUtc = dto.CreateTimeUtc,
                    DeviceId = dto.DeviceId,
                    DeviceName = dto.DeviceName,
                    TaskStatus = dto.TaskStatus,
                    Title = dto.Title,
                    Detail = dto.Detail,
                    CoverUrl = dto.CoverUrl,
                    DesignId = dto.DesignId,
                    DesignTitle = dto.DesignTitle,
                    RawJson = JsonSerializer.Serialize(dto),
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    SourceUpdatedAtUtc = DateTime.UtcNow
                };

                messages.Add(msg);
            }

            await context.PrinterMessages.AddRangeAsync(messages);
            await context.SaveChangesAsync();
            Console.WriteLine($"[PrinterSeeder] SUCCESS: Seeded {messages.Count} messages.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PrinterSeeder] ERROR: Exception during message seeding: {ex.Message}");
        }
    }

    public class PrinterDto
    {
        public string DeviceId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsOnline { get; set; }
        public string? PrintStatus { get; set; }
        public string? ModelName { get; set; }
        public string? ProductName { get; set; }
        public string? Structure { get; set; }
        public decimal? NozzleDiameterMm { get; set; }
        public int AmsUnitCount { get; set; }
    }

    public class TaskDto
    {
        public long ExternalTaskId { get; set; }
        public string? TaskAlias { get; set; }
        public string DeviceId { get; set; } = null!;
        public string? DeviceName { get; set; }
        public string? DeviceModel { get; set; }
        public string? DesignTitle { get; set; }
        public string? DesignTitleTranslated { get; set; }
        public DateTimeOffset? StartTimeUtc { get; set; }
        public DateTimeOffset? EndTimeUtc { get; set; }
        public int? CostTimeSeconds { get; set; }
        public int? LengthMm { get; set; }
        public decimal? WeightGrams { get; set; }
        public string? BedType { get; set; }
        public string? Mode { get; set; }
        public int? FailedType { get; set; }
        public string? CoverUrl { get; set; }
        public List<TaskAmsDetailDto>? AmsDetails { get; set; }
    }

    public class TaskAmsDetailDto
    {
        public int? Ams { get; set; }
        public int? AmsId { get; set; }
        public int? SlotId { get; set; }
        public int? NozzleId { get; set; }
        public string? FilamentId { get; set; }
        public string? FilamentType { get; set; }
        public string? TargetFilamentType { get; set; }
        public string? SourceColor { get; set; }
        public string? TargetColor { get; set; }
        public decimal? WeightGrams { get; set; }
    }

    public class MessageDto
    {
        public long ExternalMessageId { get; set; }
        public long? ExternalTaskId { get; set; }
        public int? Type { get; set; }
        public int? IsRead { get; set; }
        public DateTimeOffset? CreateTimeUtc { get; set; }
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public int? TaskStatus { get; set; }
        public string? Title { get; set; }
        public string? Detail { get; set; }
        public string? CoverUrl { get; set; }
        public int? DesignId { get; set; }
        public string? DesignTitle { get; set; }
    }
}