using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

/// <summary>
/// Internal endpoints called by the Discord bot (not the dashboard).
/// Protected by X-Bot-Api-Key header.
/// </summary>
[AllowAnonymous]
[BotApiKey]
[ApiController]
[Route("api/bot/guilds")]
public class BotGuildsController : ControllerBase
{
    private readonly IGuildService _guildService;
    private readonly IGuildResourceService _resourceService;
    private readonly IModuleService _moduleService;

    public BotGuildsController(
        IGuildService guildService,
        IGuildResourceService resourceService,
        IModuleService moduleService)
    {
        _guildService = guildService;
        _resourceService = resourceService;
        _moduleService = moduleService;
    }

    /// <summary>
    /// Registers or updates a guild when the bot joins a server.
    /// </summary>
    [HttpPost("join")]
    public async Task<ActionResult<RegisterGuildResponse>> RegisterGuild(
        [FromBody] RegisterGuildRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId)
            || string.IsNullOrWhiteSpace(request.Name)
            || string.IsNullOrWhiteSpace(request.OwnerDiscordUserId))
        {
            return BadRequest(new { message = "DiscordGuildId, Name, and OwnerDiscordUserId are required." });
        }

        var result = await _guildService.RegisterGuildAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns guild settings by Discord snowflake id.
    /// </summary>
    [HttpGet("{discordGuildId}/settings")]
    public async Task<ActionResult<GuildSettingsDto>> GetSettingsByDiscordGuildId(
        string discordGuildId,
        CancellationToken cancellationToken)
    {
        var settings = await _guildService.GetSettingsByDiscordGuildIdAsync(discordGuildId, cancellationToken);
        if (settings is null)
        {
            return NotFound(new { message = "Guild or settings not found." });
        }

        return Ok(settings);
    }

    /// <summary>
    /// Returns Discord guild ids that requested a resource sync from the dashboard.
    /// </summary>
    [HttpGet("sync-requests")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetSyncRequests(
        CancellationToken cancellationToken)
    {
        var guildIds = await _resourceService.GetPendingSyncDiscordGuildIdsAsync(cancellationToken);
        return Ok(guildIds);
    }

    /// <summary>
    /// Stores synced channels and roles from the bot.
    /// </summary>
    [HttpPost("{discordGuildId}/resources")]
    [HttpPost("{discordGuildId}/sync-resources")]
    public async Task<IActionResult> SyncResources(
        string discordGuildId,
        [FromBody] SyncResourcesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Channels.Count == 0 && request.Roles.Count == 0)
        {
            return BadRequest(new { message = "At least one channel or role is required." });
        }

        var success = await _resourceService.SyncResourcesAsync(discordGuildId, request, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(new { message = "Resources synced." });
    }

    /// <summary>
    /// Returns whether a module is enabled for a guild (used by the bot before running features).
    /// </summary>
    [HttpGet("{discordGuildId}/modules/{moduleKey}")]
    public async Task<ActionResult<GuildModuleStatusDto>> GetModuleStatus(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken)
    {
        var status = await _moduleService.GetModuleStatusAsync(discordGuildId, moduleKey, cancellationToken);
        if (status is null)
        {
            return NotFound(new { message = "Guild or module not found." });
        }

        return Ok(status);
    }
}
