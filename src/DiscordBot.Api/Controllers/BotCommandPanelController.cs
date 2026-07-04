using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous]
[BotApiKey]
[ApiController]
[Route("api/bot/command-panels")]
public class BotCommandPanelController : ControllerBase
{
    private readonly ICommandPanelService _commandPanelService;

    public BotCommandPanelController(ICommandPanelService commandPanelService)
    {
        _commandPanelService = commandPanelService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<CommandPanelRefreshDto>>> GetPending(
        CancellationToken cancellationToken)
    {
        var pending = await _commandPanelService.GetPendingRefreshesAsync(cancellationToken);
        return Ok(pending);
    }

    [HttpPost("{panelId:guid}/ack")]
    public async Task<IActionResult> Acknowledge(
        Guid panelId,
        [FromBody] AckCommandPanelRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _commandPanelService.AcknowledgeRefreshAsync(
            panelId,
            request,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Panel not found." });
        }

        return Ok(new { message = "Command panel refresh acknowledged." });
    }

    [HttpGet("{panelId:guid}/buttons/{buttonId:guid}")]
    public async Task<ActionResult<PanelButtonActionDto>> GetButtonAction(
        Guid panelId, Guid buttonId, CancellationToken cancellationToken)
    {
        var action = await _commandPanelService.GetButtonActionAsync(panelId, buttonId, cancellationToken);
        return action is null ? NotFound(new { message = "Panel button not found or disabled." }) : Ok(action);
    }
}
