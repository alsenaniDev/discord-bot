using DiscordBot.Api.Extensions;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize, ApiController, Route("api/guilds/{guildId:guid}")]
public class GuildWorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflows; private readonly IGuildAccessService _access;
    public GuildWorkflowsController(IWorkflowService workflows, IGuildAccessService access) { _workflows = workflows; _access = access; }
    [HttpGet("workflows")] public async Task<IActionResult> List(Guid guildId, CancellationToken ct) => await Allowed(guildId, ct) ? Ok(await _workflows.ListAsync(guildId, ct)) : NotFound();
    [HttpGet("workflows/{workflowId:guid}")] public async Task<IActionResult> Get(Guid guildId, Guid workflowId, CancellationToken ct) { if (!await Allowed(guildId, ct)) return NotFound(); var x = await _workflows.GetAsync(guildId, workflowId, ct); return x is null ? NotFound() : Ok(x); }
    [HttpPost("workflows")] public async Task<IActionResult> Create(Guid guildId, SaveWorkflowRequest request, CancellationToken ct) { if (!await Allowed(guildId, ct)) return NotFound(); var x = await _workflows.CreateAsync(guildId, request, ct); return x.Error is null ? CreatedAtAction(nameof(Get), new { guildId, workflowId = x.Value!.Id }, x.Value) : BadRequest(new { message = x.Error }); }
    [HttpPut("workflows/{workflowId:guid}")] public async Task<IActionResult> Update(Guid guildId, Guid workflowId, SaveWorkflowRequest request, CancellationToken ct) { if (!await Allowed(guildId, ct)) return NotFound(); var x = await _workflows.UpdateAsync(guildId, workflowId, request, ct); if (x.Error is not null) return BadRequest(new { message = x.Error }); return x.Value is null ? NotFound() : Ok(x.Value); }
    [HttpDelete("workflows/{workflowId:guid}")] public async Task<IActionResult> Delete(Guid guildId, Guid workflowId, CancellationToken ct) { if (!await Allowed(guildId, ct)) return NotFound(); var x = await _workflows.DeleteAsync(guildId, workflowId, ct); if (!x.Found) return NotFound(); return x.Error is null ? NoContent() : Conflict(new { message = x.Error }); }
    [HttpGet("workflow-submissions")] public async Task<IActionResult> Submissions(Guid guildId, [FromQuery] WorkflowSubmissionStatus? status, [FromQuery] Guid? workflowId, CancellationToken ct) => await Allowed(guildId, ct) ? Ok(await _workflows.ListSubmissionsAsync(guildId, status, workflowId, ct)) : NotFound();
    [HttpGet("workflow-submissions/{submissionId:guid}")] public async Task<IActionResult> Submission(Guid guildId, Guid submissionId, CancellationToken ct) { if (!await Allowed(guildId, ct)) return NotFound(); var x = await _workflows.GetSubmissionAsync(guildId, submissionId, ct); return x is null ? NotFound() : Ok(x); }
    [HttpPost("workflow-submissions/{submissionId:guid}/approve")] public Task<IActionResult> Approve(Guid guildId, Guid submissionId, ReviewWorkflowSubmissionRequest request, CancellationToken ct) => Review(guildId, submissionId, true, request, ct);
    [HttpPost("workflow-submissions/{submissionId:guid}/reject")] public Task<IActionResult> Reject(Guid guildId, Guid submissionId, ReviewWorkflowSubmissionRequest request, CancellationToken ct) => Review(guildId, submissionId, false, request, ct);
    private async Task<IActionResult> Review(Guid guildId, Guid submissionId, bool approve, ReviewWorkflowSubmissionRequest request, CancellationToken ct) { if (!await Allowed(guildId, ct)) return NotFound(); var userId = User.GetDiscordUserId()!; var x = await _workflows.ReviewAsync(guildId, submissionId, approve, userId, request, ct); if (x.Error is not null) return BadRequest(new { message = x.Error }); return x.Value is null ? NotFound() : Ok(x.Value); }
    private async Task<bool> Allowed(Guid guildId, CancellationToken ct) { var id = User.GetDiscordUserId(); if (string.IsNullOrWhiteSpace(id)) return false; return (await _access.GetAccessAsync(guildId, id, ct))?.CanManageSettings == true; }
}
