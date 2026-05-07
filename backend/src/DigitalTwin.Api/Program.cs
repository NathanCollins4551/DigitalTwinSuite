using Microsoft.EntityFrameworkCore;
using DigitalTwin.Api.HostedServices;
using DigitalTwin.Infrastructure;
using DigitalTwin.Infrastructure.Sync;
using DigitalTwin.Infrastructure.Queries;
using DigitalTwin.Infrastructure.Simulation;
using DigitalTwin.Api.Streaming;
using DigitalTwin.Application.Abstractions.Telemetry;
using DigitalTwin.Infrastructure.Telemetry;
using DigitalTwin.Infrastructure.Scheduling;
using DigitalTwin.Application.Abstractions.Inventory;
using DigitalTwin.Infrastructure.Inventory;
using DigitalTwin.Infrastructure.Persistence;
using DigitalTwin.Api.Seeds;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<PrinterCatalogSyncService>();
builder.Services.AddScoped<PrinterActivitySyncService>();
builder.Services.AddScoped<PrinterReadService>();
builder.Services.AddScoped<PrinterSimulationService>();
builder.Services.AddScoped<TaskTelemetrySummaryService>();
builder.Services.AddScoped<ScheduledPrintJobReadService>();
builder.Services.AddScoped<PrintSchedulerService>();

builder.Services.AddSingleton<IPrinterTelemetryPublisher, InMemoryTelemetryPublisher>();
builder.Services.AddSingleton<IPrinterTelemetryWriter, InfluxPrinterTelemetryWriter>();
builder.Services.AddSingleton<PrinterTelemetryGenerator>();

if (builder.Configuration.GetValue<bool>("Workers:EnablePrinterCatalogSyncWorker", false))
{
    builder.Services.AddHostedService<PrinterCatalogSyncWorker>();
}
builder.Services.AddHostedService<PrinterSimulationCompletionWorker>();
builder.Services.AddHostedService<PrinterSimulationTelemetryWorker>();
builder.Services.AddHostedService<PrinterSimulationManagerWorker>();

if (builder.Configuration.GetValue<bool>("Workers:EnablePrintSchedulingWorker", false))
{
    builder.Services.AddHostedService<PrintSchedulingWorker>();
}
builder.Services.AddSingleton<IZoneInventoryPublisher, InMemoryZoneInventoryPublisher>();
builder.Services.AddScoped<CvZoneStateService>();
builder.Services.AddScoped<PrinterLoadedSpoolSeeder>();

if (builder.Configuration.GetValue<bool>("Workers:EnableCvEventsConsumerWorker", false))
{
    builder.Services.AddHostedService<CvEventsConsumerWorker>();
}

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var db = scope.ServiceProvider.GetRequiredService<DigitalTwin.Infrastructure.Persistence.DigitalTwinDbContext>();
    var spoolSeeder = scope.ServiceProvider.GetRequiredService<PrinterLoadedSpoolSeeder>();

    logger.LogInformation("Applying database migrations...");
    await db.Database.MigrateAsync();

    // 1. Seed Printers, Tasks, and Messages FIRST
    logger.LogInformation("Seeding printer metadata (printers, tasks, messages)...");
    await PrinterSeeder.SeedEverythingAsync(db, app.Environment.ContentRootPath);

    // 2. Seed Printer Loaded Spools SECOND (depends on printers existing)
    logger.LogInformation("Seeding printer_loaded_spools...");
    await spoolSeeder.SeedAsync();
}

app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();