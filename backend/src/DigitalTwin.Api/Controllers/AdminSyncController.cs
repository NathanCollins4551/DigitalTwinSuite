using DigitalTwin.Infrastructure.Sync;
using Microsoft.AspNetCore.Mvc;

namespace DigitalTwin.Api.Controllers;

[ApiController]
[Route("api/admin/sync")]
public class AdminSyncController : ControllerBase
{
    [HttpPost("catalog")]
    public async Task<IActionResult> SyncCatalog(
        [FromServices] PrinterCatalogSyncService syncService,
        CancellationToken cancellationToken)
    {
        await syncService.SyncBindAsync(cancellationToken);
        await syncService.SyncVersionAsync(cancellationToken);

        return Ok(new { message = "Catalog sync completed." });
    }

    [HttpPost("activity")]
    public async Task<IActionResult> SyncActivity(
        [FromServices] PrinterActivitySyncService syncService,
        CancellationToken cancellationToken)
    {
        await syncService.SyncTasksAsync(cancellationToken);
        await syncService.SyncMessagesAsync(cancellationToken);

        return Ok(new { message = "Activity sync completed." });
    }

    [HttpPost("all")]
    public async Task<IActionResult> SyncAll(
        [FromServices] PrinterCatalogSyncService catalogSync,
        [FromServices] PrinterActivitySyncService activitySync,
        CancellationToken cancellationToken)
    {
        await catalogSync.SyncBindAsync(cancellationToken);
        await catalogSync.SyncVersionAsync(cancellationToken);
        await activitySync.SyncTasksAsync(cancellationToken);
        await activitySync.SyncMessagesAsync(cancellationToken);

        return Ok(new { message = "Full sync completed." });
    }
}