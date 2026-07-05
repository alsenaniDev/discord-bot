using DiscordBot.Api.Filters;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous, BotApiKey, ApiController, Route("api/bot")]
public class BotWorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflows;
    public BotWorkflowsController(IWorkflowService workflows) { _workflows = workflows; }
    [HttpGet("workflows/{workflowId:guid}/start-context")]
    public async Task<IActionResult> Start(Guid workflowId, [FromQuery] string discordGuildId, [FromQuery] string discordUserId, CancellationToken ct) { var x = await _workflows.GetStartContextAsync(workflowId, discordGuildId, discordUserId, ct); return x is null ? NotFound() : Ok(x); }
    [HttpPost("workflows/{workflowId:guid}/submissions")]
    public async Task<IActionResult> Submit(Guid workflowId, CreateWorkflowSubmissionRequest request, CancellationToken ct) { var x = await _workflows.SubmitAsync(workflowId, request, ct); return x.Error is null ? Ok(x.Value) : BadRequest(new { message = x.Error }); }
    [HttpGet("workflows/pending-actions")] public async Task<IActionResult> Pending(CancellationToken ct) => Ok(await _workflows.GetPendingActionsAsync(ct));
    [HttpPost("workflows/pending-actions/{actionId:guid}/ack")]
    public async Task<IActionResult> Ack(Guid actionId, AckWorkflowPendingActionRequest request, CancellationToken ct) => await _workflows.AckPendingActionAsync(actionId, request, ct) ? Ok() : NotFound();
}
