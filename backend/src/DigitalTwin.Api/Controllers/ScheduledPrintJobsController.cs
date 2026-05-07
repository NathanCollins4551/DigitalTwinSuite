using DigitalTwin.Application.Printers.Dtos;
using DigitalTwin.Infrastructure.Queries;
using DigitalTwin.Infrastructure.Scheduling;
using Microsoft.AspNetCore.Mvc;

namespace DigitalTwin.Api.Controllers;

[ApiController]
[Route("api/scheduled-print-jobs")]
public class ScheduledPrintJobsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateScheduledPrintJobRequest request,
        [FromServices] ScheduledPrintJobReadService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { jobId = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? status,
        [FromServices] ScheduledPrintJobReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{jobId:guid}")]
    public async Task<IActionResult> GetById(
        Guid jobId,
        [FromServices] ScheduledPrintJobReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(jobId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{jobId:guid}/cancel")]
    public async Task<IActionResult> Cancel(
        Guid jobId,
        [FromServices] ScheduledPrintJobReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.CancelAsync(jobId, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(new { message = result.Message });
    }

    // [HttpPost("run-dispatch-cycle")]
    // public async Task<IActionResult> RunDispatchCycle(
    //     [FromServices] PrintSchedulerService scheduler,
    //     CancellationToken cancellationToken)
    // {
    //     await scheduler.RunDispatchCycleAsync(cancellationToken);
    //     return Ok(new { message = "Dispatch cycle completed." });
    // }

    [HttpPost("{jobId:guid}/priority")]
    public async Task<IActionResult> UpdatePriority(
        Guid jobId,
        [FromBody] UpdateScheduledPrintJobPriorityRequest request,
        [FromServices] ScheduledPrintJobReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdatePriorityAsync(jobId, request.Priority, cancellationToken);

        if (!result.Success)
            return BadRequest(new { message = result.Message });

        return Ok(result.Job);
    }

    [HttpGet("queue-preview")]
    public async Task<IActionResult> GetQueuePreview(
        [FromServices] ScheduledPrintJobReadService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetQueuePreviewAsync(cancellationToken);
        return Ok(result);
    }

    // [HttpPost("reconcile")]
    // public async Task<IActionResult> ReconcileRunningJobs(
    //     [FromServices] PrintSchedulerService scheduler,
    //     CancellationToken cancellationToken)
    // {
    //     await scheduler.ReconcileRunningJobsAsync(cancellationToken);
    //     return Ok(new { message = "Reconcile completed." });
    // }

    // [HttpGet("scheduler-control")]
    // public async Task<IActionResult> GetSchedulerControl(
    //     [FromServices] ScheduledPrintJobReadService service,
    //     CancellationToken cancellationToken)
    // {
    //     var result = await service.GetSchedulerControlAsync(cancellationToken);
    //     return Ok(result);
    // }

    // [HttpPost("scheduler-control")]
    // public async Task<IActionResult> UpdateSchedulerControl(
    //     [FromBody] UpdateSchedulerPauseRequest request,
    //     [FromServices] ScheduledPrintJobReadService service,
    //     CancellationToken cancellationToken)
    // {
    //     var result = await service.UpdateSchedulerControlAsync(
    //         request.IsPaused,
    //         request.PauseReason,
    //         cancellationToken);

    //     return Ok(result);
    // }
}