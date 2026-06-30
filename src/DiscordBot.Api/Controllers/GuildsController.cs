using DiscordBot.Api.Extensions;
using DiscordBot.Api.Validation;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/guilds")]
public class GuildsController : ControllerBase
{
    private readonly IGuildService _guildService;
    private readonly IGuildResourceService _resourceService;
    private readonly ITicketService _ticketService;
    private readonly IModerationService _moderationService;
    private readonly IModuleService _moduleService;
    private readonly ILogService _logService;
    private readonly IReactionRoleService _reactionRoleService;
    private readonly ISubscriptionService _subscriptionService;

    public GuildsController(
        IGuildService guildService,
        IGuildResourceService resourceService,
        ITicketService ticketService,
        IModerationService moderationService,
        IModuleService moduleService,
        ILogService logService,
        IReactionRoleService reactionRoleService,
        ISubscriptionService subscriptionService)
    {
        _guildService = guildService;
        _resourceService = resourceService;
        _ticketService = ticketService;
        _moderationService = moderationService;
        _moduleService = moduleService;
        _logService = logService;
        _reactionRoleService = reactionRoleService;
        _subscriptionService = subscriptionService;
    }

    /// <summary>
    /// Lists guilds owned by the logged-in Discord user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GuildSummaryDto>>> GetGuilds(
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var guilds = await _guildService.GetAccessibleGuildsAsync(discordUserId, cancellationToken);
        return Ok(guilds);
    }

    /// <summary>
    /// Returns a summary overview for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/overview")]
    public async Task<ActionResult<GuildOverviewDto>> GetOverview(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var overview = await _guildService.GetOverviewAsync(id, discordUserId, cancellationToken);
        if (overview is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(overview);
    }

    /// <summary>
    /// Returns settings for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/settings")]
    public async Task<ActionResult<GuildSettingsDto>> GetSettings(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var settings = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
        if (settings is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(settings);
    }

    /// <summary>
    /// Updates settings for a guild the user owns.
    /// </summary>
    [HttpPut("{id:guid}/settings")]
    public async Task<ActionResult<GuildSettingsDto>> UpdateSettings(
        Guid id,
        [FromBody] UpdateGuildSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = GuildSettingsValidator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new { message = "Validation failed.", errors = validationErrors });
        }

        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var settings = await _guildService.UpdateSettingsAsync(id, discordUserId, request, cancellationToken);
        if (settings is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(settings);
    }

    /// <summary>
    /// Lists tickets for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/tickets")]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetTickets(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var tickets = await _ticketService.GetGuildTicketsAsync(id, discordUserId, cancellationToken);
        if (tickets.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(tickets);
    }

    /// <summary>
    /// Lists synced Discord channels for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/channels")]
    public async Task<ActionResult<IReadOnlyList<DiscordChannelDto>>> GetChannels(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var channels = await _resourceService.GetChannelsAsync(id, discordUserId, cancellationToken);
        if (channels.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(channels);
    }

    /// <summary>
    /// Lists synced Discord roles for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/roles")]
    public async Task<ActionResult<IReadOnlyList<DiscordRoleDto>>> GetRoles(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var roles = await _resourceService.GetRolesAsync(id, discordUserId, cancellationToken);
        if (roles.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(roles);
    }

    /// <summary>
    /// Lists synced Discord category channels for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/categories")]
    public async Task<ActionResult<IReadOnlyList<DiscordChannelDto>>> GetCategories(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var categories = await _resourceService.GetCategoriesAsync(id, discordUserId, cancellationToken);
        if (categories.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(categories);
    }

    /// <summary>
    /// Requests the bot to sync Discord channels and roles for this guild.
    /// </summary>
    [HttpPost("{id:guid}/sync-resources")]
    public async Task<ActionResult<RequestResourceSyncResponse>> RequestResourceSync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var result = await _resourceService.RequestSyncAsync(id, discordUserId, cancellationToken);
        if (result is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(result);
    }

    /// <summary>
    /// Lists warnings for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/warnings")]
    public async Task<ActionResult<IReadOnlyList<WarningDto>>> GetWarnings(
        Guid id,
        [FromQuery] string? targetUserId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var filter = new ModerationListFilter
        {
            TargetUserId = targetUserId,
            From = from,
            To = to
        };

        var warnings = await _moderationService.GetWarningsAsync(id, discordUserId, filter, cancellationToken);
        if (warnings.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(warnings);
    }

    /// <summary>
    /// Lists moderation cases for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/moderation-cases")]
    public async Task<ActionResult<IReadOnlyList<ModerationCaseDto>>> GetModerationCases(
        Guid id,
        [FromQuery] string? targetUserId,
        [FromQuery] ModerationCaseType? type,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var filter = new ModerationListFilter
        {
            TargetUserId = targetUserId,
            Type = type,
            From = from,
            To = to
        };

        var cases = await _moderationService.GetCasesAsync(id, discordUserId, filter, cancellationToken);
        if (cases.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(cases);
    }

    /// <summary>
    /// Lists module enable/disable state for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/modules")]
    public async Task<ActionResult<IReadOnlyList<GuildModuleDto>>> GetModules(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var modules = await _moduleService.GetGuildModulesAsync(id, discordUserId, cancellationToken);
        if (modules.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(modules);
    }

    /// <summary>
    /// Enables or disables a module for a guild the user owns.
    /// </summary>
    [HttpPut("{id:guid}/modules/{moduleKey}")]
    public async Task<ActionResult<GuildModuleDto>> UpdateModule(
        Guid id,
        string moduleKey,
        [FromBody] UpdateGuildModuleRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var result = await _moduleService.UpdateGuildModuleAsync(
            id,
            discordUserId,
            moduleKey,
            request.IsEnabled,
            cancellationToken);

        if (result.ErrorCode == "MODULE_NOT_IN_PLAN")
        {
            return BadRequest(new { message = "This module is not included in the current subscription plan." });
        }

        if (result.Module is null)
        {
            return NotFound(new { message = "Guild or module not found, or access denied." });
        }

        return Ok(result.Module);
    }

    /// <summary>
    /// Lists activity logs for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/logs")]
    public async Task<ActionResult<IReadOnlyList<LogEntryDto>>> GetLogs(
        Guid id,
        [FromQuery] LogEventType? type,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var filter = new LogListFilter
        {
            Type = type,
            From = from,
            To = to,
            Search = search
        };

        var logs = await _logService.GetLogsAsync(id, discordUserId, filter, cancellationToken);
        if (logs.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(logs);
    }

    /// <summary>
    /// Lists reaction role panels for a guild the user owns.
    /// </summary>
    [HttpGet("{id:guid}/reaction-roles")]
    public async Task<ActionResult<IReadOnlyList<ReactionRoleDto>>> GetReactionRoles(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var panels = await _reactionRoleService.GetGuildReactionRolesAsync(id, discordUserId, cancellationToken);
        if (panels.Count == 0)
        {
            var guildExists = await _guildService.GetSettingsAsync(id, discordUserId, cancellationToken);
            if (guildExists is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(panels);
    }

    /// <summary>
    /// Deactivates a reaction role panel for a guild the user owns.
    /// </summary>
    [HttpDelete("{id:guid}/reaction-roles/{reactionRoleId:guid}")]
    public async Task<IActionResult> DeactivateReactionRole(
        Guid id,
        Guid reactionRoleId,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var success = await _reactionRoleService.DeactivateAsync(id, reactionRoleId, discordUserId, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Reaction role not found, already inactive, or access denied." });
        }

        return Ok(new { message = "Reaction role deactivated." });
    }

    [HttpGet("{id:guid}/subscription")]
    public async Task<ActionResult<GuildSubscriptionDto>> GetSubscription(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var subscription = await _subscriptionService.GetGuildSubscriptionAsync(id, discordUserId, cancellationToken);
        if (subscription is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(subscription);
    }

    [HttpPut("{id:guid}/subscription")]
    public async Task<ActionResult<GuildSubscriptionDto>> UpdateSubscription(
        Guid id,
        [FromBody] UpdateGuildSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        if (string.IsNullOrWhiteSpace(request.PlanKey))
        {
            return BadRequest(new { message = "PlanKey is required." });
        }

        var subscription = await _subscriptionService.UpdateGuildSubscriptionAsync(
            id,
            discordUserId,
            request.PlanKey,
            cancellationToken);

        if (subscription is null)
        {
            return NotFound(new { message = "Guild or plan not found, or access denied." });
        }

        return Ok(subscription);
    }
}
