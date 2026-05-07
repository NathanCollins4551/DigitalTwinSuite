using DigitalTwin.Domain.Entities;
using DigitalTwin.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DigitalTwin.Infrastructure.Inventory;

public class PrinterLoadedSpoolSeeder
{
    private readonly DigitalTwinDbContext _db;

    public PrinterLoadedSpoolSeeder(DigitalTwinDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var printers = await _db.Printers
            .Include(x => x.AmsUnits)
            .ToListAsync(cancellationToken);

        if (printers.Count == 0)
            return;

        var now = DateTimeOffset.UtcNow;

        var templates = new[]
        {
            new { SlotIndex = 0, SpoolCode = "FIL-PETG-RED",   MaterialType = "PETG", ColorName = "red",   ColorHex = "#FF0000", RemainingPercent = 80m, RemainingGrams = 800m },
            new { SlotIndex = 1, SpoolCode = "FIL-PETG-BLUE",  MaterialType = "PETG", ColorName = "blue",  ColorHex = "#0000FF", RemainingPercent = 70m, RemainingGrams = 700m },
            new { SlotIndex = 2, SpoolCode = "FIL-PETG-BLACK", MaterialType = "PETG", ColorName = "black", ColorHex = "#000000", RemainingPercent = 60m, RemainingGrams = 600m },
            new { SlotIndex = 3, SpoolCode = "FIL-PETG-GREEN", MaterialType = "PETG", ColorName = "green", ColorHex = "#00FF00", RemainingPercent = 50m, RemainingGrams = 500m }
        };

        foreach (var printer in printers)
        {
            var existing = await _db.PrinterLoadedSpools
                .Where(x => x.PrinterId == printer.Id && x.IsActive)
                .ToListAsync(cancellationToken);

            if (existing.Count > 0)
                continue;

            var firstAmsUnitId = printer.AmsUnits
                .OrderBy(x => x.AmsIndex)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefault();

            foreach (var template in templates)
            {
                _db.PrinterLoadedSpools.Add(new PrinterLoadedSpool
                {
                    Id = Guid.NewGuid(),
                    PrinterId = printer.Id,
                    PrinterAmsUnitId = firstAmsUnitId,
                    SlotIndex = template.SlotIndex,
                    SpoolCode = template.SpoolCode,
                    MaterialType = template.MaterialType,
                    ColorName = template.ColorName,
                    ColorHex = template.ColorHex,
                    RemainingPercent = template.RemainingPercent,
                    RemainingGrams = template.RemainingGrams,
                    IsActive = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}