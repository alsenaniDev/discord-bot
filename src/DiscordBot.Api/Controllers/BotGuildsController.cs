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
    private readonly IGuildPermissionResolver _permissionResolver;
    private readonly IModerationPermissionResolver _moderationPermissionResolver;
    private readonly IGuildProfileService _guildProfileService;
    private readonly IAutoReplyService _autoReplyService;

    public BotGuildsController(
        IGuildService guildService,
        IGuildResourceService resourceService,
        IModuleService moduleService,
        IGuildPermissionResolver permissionResolver,
        IModerationPermissionResolver moderationPermissionResolver,
        IGuildProfileService guildProfileService,
        IAutoReplyService autoReplyService)
    {
        _guildService = guildService;
        _resourceService = resourceService;
        _moduleService = moduleService;
        _permissionResolver = permissionResolver;
        _moderationPermissionResolver = moderationPermissionResolver;
        _guildProfileService = guildProfileService;
        _autoReplyService = autoReplyService;
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

    [HttpGet("{discordGuildId}/auto-replies")]
    public async Task<ActionResult<IReadOnlyList<AutoReplyRuleDto>>> GetAutoReplies(
        string discordGuildId,
        CancellationToken cancellationToken)
    {
        var rules = await _autoReplyService.GetEnabledRulesByDiscordGuildIdAsync(discordGuildId, cancellationToken);
        return Ok(rules);
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
        if (request.Channels.Count == 0 && request.Roles.Count == 0 && request.Members.Count == 0)
        {
            return BadRequest(new { message = "At least one channel, role, or member is required." });
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

    [HttpPost("{discordGuildId}/permissions/evaluate")]
    public async Task<ActionResult<EvaluatePermissionsResponse>> EvaluatePermissions(
        string discordGuildId,
        [FromBody] EvaluatePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordUserId))
        {
            return BadRequest(new { message = "DiscordUserId is required." });
        }

        var resolved = await _moderationPermissionResolver.ResolveByDiscordGuildIdAsync(
            discordGuildId,
            request.DiscordUserId,
            request.DiscordRoleIds,
            cancellationToken);

        if (resolved is null)
        {
            return Ok(new EvaluatePermissionsResponse());
        }

        return Ok(new EvaluatePermissionsResponse
        {
            CanWarn = resolved.CanWarn,
            CanKick = resolved.CanKick,
            CanTimeout = false,
            CanClearMessages = resolved.CanClearMessages,
            CanAccessModeration = resolved.CanAccessModeration,
            CanViewWarnings = resolved.CanViewWarnings,
            CanViewModerationCases = resolved.CanViewModerationCases,
            CanViewLogs = resolved.CanViewLogs
        });
    }

    [HttpGet("{discordGuildId}/profile")]
    public async Task<ActionResult<GuildProfileDto>> GetProfile(
        string discordGuildId,
        CancellationToken cancellationToken)
    {
        var profile = await _guildProfileService.GetProfileByDiscordGuildIdAsync(discordGuildId, cancellationToken);
        if (profile is null)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(profile);
    }

    [HttpPost("{discordGuildId}/dashboard-access/evaluate")]
    public async Task<ActionResult<EvaluateDashboardAccessResponse>> EvaluateDashboardAccess(
        string discordGuildId,
        [FromBody] EvaluatePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordUserId))
        {
            return BadRequest(new { message = "DiscordUserId is required." });
        }

        var resolved = await _permissionResolver.ResolveByDiscordGuildIdAsync(
            discordGuildId,
            request.DiscordUserId,
            request.DiscordRoleIds,
            cancellationToken);

        if (resolved is null)
        {
            return Ok(new EvaluateDashboardAccessResponse());
        }

        var access = GuildPermissionMapper.ToAccessDto(resolved);
        return Ok(new EvaluateDashboardAccessResponse
        {
            CanAccessTickets = access.CanAccessTickets,
            CanAccessLogs = access.CanAccessLogs,
            CanAccessModeration = access.CanAccessModeration
        });
    }
}
