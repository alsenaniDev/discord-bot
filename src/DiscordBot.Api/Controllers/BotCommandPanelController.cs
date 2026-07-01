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

    [HttpPost("{discordGuildId}/ack")]
    public async Task<IActionResult> Acknowledge(
        string discordGuildId,
        [FromBody] AckCommandPanelRequest request,
        CancellationToken cancellationToken)
    {
        var success = await _commandPanelService.AcknowledgeRefreshAsync(
            discordGuildId,
            request,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(new { message = "Command panel refresh acknowledged." });
    }
}
