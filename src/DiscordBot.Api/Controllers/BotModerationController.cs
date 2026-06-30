using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[AllowAnonymous]
[BotApiKey]
[ApiController]
[Route("api/bot/moderation")]
public class BotModerationController : ControllerBase
{
    private readonly IModerationService _moderationService;

    public BotModerationController(IModerationService moderationService)
    {
        _moderationService = moderationService;
    }

    [HttpPost("warnings")]
    public async Task<ActionResult<WarningDto>> CreateWarning(
        [FromBody] CreateWarningRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId)
            || string.IsNullOrWhiteSpace(request.TargetDiscordUserId)
            || string.IsNullOrWhiteSpace(request.ModeratorDiscordUserId)
            || string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { message = "DiscordGuildId, TargetDiscordUserId, ModeratorDiscordUserId, and Reason are required." });
        }

        var warning = await _moderationService.CreateWarningAsync(request, cancellationToken);
        if (warning is null)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(warning);
    }

    [HttpPost("cases")]
    public async Task<ActionResult<ModerationCaseDto>> CreateCase(
        [FromBody] CreateModerationCaseRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId)
            || string.IsNullOrWhiteSpace(request.ModeratorDiscordUserId))
        {
            return BadRequest(new { message = "DiscordGuildId and ModeratorDiscordUserId are required." });
        }

        var moderationCase = await _moderationService.CreateCaseAsync(request, cancellationToken);
        if (moderationCase is null)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(moderationCase);
    }

    [HttpGet("warnings")]
    public async Task<ActionResult<IReadOnlyList<WarningDto>>> GetWarningsForUser(
        [FromQuery] string discordGuildId,
        [FromQuery] string targetUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(discordGuildId) || string.IsNullOrWhiteSpace(targetUserId))
        {
            return BadRequest(new { message = "discordGuildId and targetUserId are required." });
        }

        var warnings = await _moderationService.GetWarningsByDiscordGuildAsync(
            discordGuildId,
            targetUserId,
            cancellationToken);

        return Ok(warnings);
    }
}
