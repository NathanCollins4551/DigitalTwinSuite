using DigitalTwin.Infrastructure.Queries;
using Microsoft.AspNetCore.Mvc;

namespace DigitalTwin.Api.Controllers;

[ApiController]
[Route("api")]
public class ActivityController : ControllerBase
{
    [HttpGet("tasks")]
    public async Task<IActionResult> GetAllTasks(
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetAllTasksAsync(cancellationToken));
    }

    [HttpGet("tasks/{externalTaskId:long}")]
    public async Task<IActionResult> GetTaskByExternalTaskId(
        long externalTaskId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        var result = await readService.GetTaskByExternalTaskIdAsync(externalTaskId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("tasks/by-alias/{taskAlias}")]
    public async Task<IActionResult> GetTaskByTaskAlias(
        string taskAlias,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        var externalTaskId = await readService.ResolveExternalTaskIdByTaskAliasAsync(taskAlias, cancellationToken);
        if (externalTaskId is null)
            return NotFound();

        var result = await readService.GetTaskByExternalTaskIdAsync(externalTaskId.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("messages")]
    public async Task<IActionResult> GetAllMessages(
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetAllMessagesAsync(cancellationToken));
    }

    [HttpGet("messages/{externalMessageId:long}")]
    public async Task<IActionResult> GetMessageByExternalMessageId(
        long externalMessageId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        var result = await readService.GetMessageByExternalMessageIdAsync(externalMessageId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("task-ams-details")]
    public async Task<IActionResult> GetAllTaskAmsDetails(
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetAllTaskAmsDetailsAsync(cancellationToken));
    }

    [HttpGet("task-ams-details/by-task/{externalTaskId:long}")]
    public async Task<IActionResult> GetTaskAmsDetailsByExternalTaskId(
        long externalTaskId,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        return Ok(await readService.GetTaskAmsDetailsByExternalTaskIdAsync(externalTaskId, cancellationToken));
    }

    [HttpGet("task-ams-details/by-task-alias/{taskAlias}")]
    public async Task<IActionResult> GetTaskAmsDetailsByTaskAlias(
        string taskAlias,
        [FromServices] PrinterReadService readService,
        CancellationToken cancellationToken)
    {
        var externalTaskId = await readService.ResolveExternalTaskIdByTaskAliasAsync(taskAlias, cancellationToken);
        if (externalTaskId is null)
            return Ok(Array.Empty<object>());

        return Ok(await readService.GetTaskAmsDetailsByExternalTaskIdAsync(externalTaskId.Value, cancellationToken));
    }

    [HttpGet("tasks/{externalTaskId:long}/summary")]
    public async Task<IActionResult> GetTaskSummary(
        long externalTaskId,
        [FromServices] TaskTelemetrySummaryService summaryService,
        CancellationToken cancellationToken)
    {
        var result = await summaryService.GetTaskSummaryAsync(externalTaskId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("tasks/by-alias/{taskAlias}/summary")]
    public async Task<IActionResult> GetTaskSummaryByTaskAlias(
        string taskAlias,
        [FromServices] PrinterReadService readService,
        [FromServices] TaskTelemetrySummaryService summaryService,
        CancellationToken cancellationToken)
    {
        var externalTaskId = await readService.ResolveExternalTaskIdByTaskAliasAsync(taskAlias, cancellationToken);
        if (externalTaskId is null)
            return NotFound();

        var result = await summaryService.GetTaskSummaryAsync(externalTaskId.Value, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
