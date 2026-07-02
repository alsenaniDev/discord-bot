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
    private readonly IGuildAccessService _guildAccessService;
    private readonly IPlanUpgradeRequestService _planUpgradeRequestService;
    private readonly IGuildStaffService _guildStaffService;
    private readonly IGuildPermissionRoleService _guildPermissionRoleService;
    private readonly IGuildProfileService _guildProfileService;
    private readonly IModerationPermissionRoleService _moderationPermissionRoleService;
    private readonly IAutoReplyService _autoReplyService;

    public GuildsController(
        IGuildService guildService,
        IGuildResourceService resourceService,
        ITicketService ticketService,
        IModerationService moderationService,
        IModuleService moduleService,
        ILogService logService,
        IReactionRoleService reactionRoleService,
        ISubscriptionService subscriptionService,
        IGuildAccessService guildAccessService,
        IPlanUpgradeRequestService planUpgradeRequestService,
        IGuildStaffService guildStaffService,
        IGuildPermissionRoleService guildPermissionRoleService,
        IGuildProfileService guildProfileService,
        IModerationPermissionRoleService moderationPermissionRoleService,
        IAutoReplyService autoReplyService)
    {
        _guildService = guildService;
        _resourceService = resourceService;
        _ticketService = ticketService;
        _moderationService = moderationService;
        _moduleService = moduleService;
        _logService = logService;
        _reactionRoleService = reactionRoleService;
        _subscriptionService = subscriptionService;
        _guildAccessService = guildAccessService;
        _planUpgradeRequestService = planUpgradeRequestService;
        _guildStaffService = guildStaffService;
        _guildPermissionRoleService = guildPermissionRoleService;
        _guildProfileService = guildProfileService;
        _moderationPermissionRoleService = moderationPermissionRoleService;
        _autoReplyService = autoReplyService;
    }

    /// <summary>
    /// Lists guilds the logged-in user can access (owned or staff).
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

        try
        {
            var settings = await _guildService.UpdateSettingsAsync(id, discordUserId, request, cancellationToken);
            if (settings is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }

            return Ok(settings);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
            var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(id, discordUserId, cancellationToken);
            if (!hasAccess)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(tickets);
    }

    /// <summary>
    /// Closes an open ticket from the dashboard.
    /// </summary>
    [HttpPatch("{id:guid}/tickets/{ticketId:guid}/close")]
    public async Task<ActionResult<TicketDto>> CloseTicket(
        Guid id,
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var ticket = await _ticketService.CloseTicketForGuildAsync(
            id,
            ticketId,
            discordUserId,
            cancellationToken);

        if (ticket is null)
        {
            return NotFound(new { message = "Ticket not found, already closed, or access denied." });
        }

        return Ok(ticket);
    }

    /// <summary>
    /// Sends a staff reply into an open ticket channel via the bot.
    /// </summary>
    [HttpPost("{id:guid}/tickets/{ticketId:guid}/messages")]
    public async Task<ActionResult<TicketOutboundMessageDto>> SendTicketMessage(
        Guid id,
        Guid ticketId,
        [FromBody] SendTicketMessageRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        try
        {
            var message = await _ticketService.SendTicketMessageAsync(
                id,
                ticketId,
                discordUserId,
                request,
                cancellationToken);

            if (message is null)
            {
                return NotFound(new { message = "Ticket not found, not open, or access denied." });
            }

            return Ok(message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/auto-replies")]
    public async Task<ActionResult<IReadOnlyList<AutoReplyRuleDto>>> GetAutoReplies(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var rules = await _autoReplyService.GetRulesAsync(id, discordUserId, cancellationToken);
        if (rules.Count == 0 && !await _guildAccessService.IsOwnerAsync(id, discordUserId, cancellationToken))
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(rules);
    }

    [HttpPost("{id:guid}/auto-replies")]
    public async Task<ActionResult<AutoReplyRuleDto>> CreateAutoReply(
        Guid id,
        [FromBody] CreateAutoReplyRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = AutoReplyValidator.ValidateCreate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new { message = "Validation failed.", errors = validationErrors });
        }

        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var rule = await _autoReplyService.CreateAsync(id, discordUserId, request, cancellationToken);
        if (rule is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(rule);
    }

    [HttpPut("{id:guid}/auto-replies/{ruleId:guid}")]
    public async Task<ActionResult<AutoReplyRuleDto>> UpdateAutoReply(
        Guid id,
        Guid ruleId,
        [FromBody] UpdateAutoReplyRuleRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = AutoReplyValidator.ValidateUpdate(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new { message = "Validation failed.", errors = validationErrors });
        }

        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var rule = await _autoReplyService.UpdateAsync(id, ruleId, discordUserId, request, cancellationToken);
        if (rule is null)
        {
            return NotFound(new { message = "Auto-reply rule not found or access denied." });
        }

        return Ok(rule);
    }

    [HttpDelete("{id:guid}/auto-replies/{ruleId:guid}")]
    public async Task<IActionResult> DeleteAutoReply(
        Guid id,
        Guid ruleId,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var success = await _autoReplyService.DeleteAsync(id, ruleId, discordUserId, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Auto-reply rule not found or access denied." });
        }

        return Ok(new { message = "Auto-reply rule removed." });
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
            var access = await _guildAccessService.GetAccessAsync(id, discordUserId, cancellationToken);
            if (access is null || !access.CanManageSettings)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(roles);
    }

    /// <summary>
    /// Lists synced Discord members for a guild the user can access.
    /// </summary>
    [HttpGet("{id:guid}/members")]
    public async Task<ActionResult<IReadOnlyList<DiscordGuildMemberDto>>> GetMembers(
        Guid id,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var members = await _resourceService.GetMembersAsync(id, discordUserId, search, cancellationToken);
        if (members.Count == 0)
        {
            var hasAccess = await _guildAccessService.GetAccessAsync(id, discordUserId, cancellationToken);
            if (hasAccess is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(members);
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
            var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(id, discordUserId, cancellationToken);
            if (!hasAccess)
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
            var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(id, discordUserId, cancellationToken);
            if (!hasAccess)
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
        [FromQuery] string? userId,
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
            Search = search,
            UserId = userId
        };

        var logs = await _logService.GetLogsAsync(id, discordUserId, filter, cancellationToken);
        if (logs.Count == 0)
        {
            var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(id, discordUserId, cancellationToken);
            if (!hasAccess)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }
        }

        return Ok(logs);
    }

    /// <summary>
    /// Deletes all activity logs for a guild. Requires typing DELETE in the request body.
    /// </summary>
    [HttpDelete("{id:guid}/logs")]
    public async Task<ActionResult<ClearLogsResult>> ClearLogs(
        Guid id,
        [FromBody] ClearLogsRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        try
        {
            var result = await _logService.ClearLogsAsync(id, discordUserId, request, cancellationToken);
            if (result is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
    public IActionResult UpdateSubscription(Guid id)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Plan changes require admin approval. Submit an upgrade request instead."
        });
    }

    [HttpGet("{id:guid}/access")]
    public async Task<ActionResult<GuildAccessDto>> GetAccess(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var access = await _guildAccessService.GetAccessAsync(id, discordUserId, cancellationToken);
        if (access is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(access);
    }

    [HttpGet("{id:guid}/subscription/upgrade-requests")]
    public async Task<ActionResult<IReadOnlyList<PlanUpgradeRequestDto>>> GetUpgradeRequests(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var requests = await _planUpgradeRequestService.GetGuildRequestsAsync(id, discordUserId, cancellationToken);
        if (requests.Count == 0 && !await _guildAccessService.IsOwnerAsync(id, discordUserId, cancellationToken))
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(requests);
    }

    [HttpPost("{id:guid}/subscription/upgrade-requests")]
    public async Task<ActionResult<PlanUpgradeRequestDto>> CreateUpgradeRequest(
        Guid id,
        [FromBody] CreatePlanUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        var userId = User.GetUserId();
        if (string.IsNullOrWhiteSpace(discordUserId) || userId is null)
        {
            return Unauthorized(new { message = "Missing user identity in token." });
        }

        if (string.IsNullOrWhiteSpace(request.PlanKey))
        {
            return BadRequest(new { message = "PlanKey is required." });
        }

        if (request.DurationMonths <= 0)
        {
            return BadRequest(new { message = "DurationMonths is required." });
        }

        try
        {
            var created = await _planUpgradeRequestService.CreateRequestAsync(
                id,
                discordUserId,
                userId.Value,
                request.PlanKey,
                request.DurationMonths,
                cancellationToken);

            if (created is null)
            {
                return NotFound(new { message = "Guild or plan not found, or access denied." });
            }

            return Ok(created);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/staff")]
    public async Task<ActionResult<IReadOnlyList<GuildStaffDto>>> GetStaff(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var staff = await _guildStaffService.GetStaffAsync(id, discordUserId, cancellationToken);
        if (staff.Count == 0 && !await _guildAccessService.CanManageStaffAsync(id, discordUserId, cancellationToken))
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(staff);
    }

    [HttpPost("{id:guid}/staff")]
    public async Task<ActionResult<GuildStaffDto>> AddStaff(
        Guid id,
        [FromBody] AddGuildStaffRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        if (string.IsNullOrWhiteSpace(request.DiscordUserId))
        {
            return BadRequest(new { message = "DiscordUserId is required." });
        }

        try
        {
            var staff = await _guildStaffService.AddStaffAsync(id, discordUserId, request, cancellationToken);
            if (staff is null)
            {
                return NotFound(new { message = "Guild not found, user invalid, or access denied." });
            }

            return Ok(staff);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/staff/{staffId:guid}")]
    public async Task<IActionResult> RemoveStaff(
        Guid id,
        Guid staffId,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var success = await _guildStaffService.RemoveStaffAsync(id, staffId, discordUserId, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Staff member not found or access denied." });
        }

        return Ok(new { message = "Staff member removed." });
    }

    [HttpGet("{id:guid}/permission-roles")]
    public async Task<ActionResult<IReadOnlyList<GuildPermissionRoleDto>>> GetPermissionRoles(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var roles = await _guildPermissionRoleService.GetRolesAsync(id, discordUserId, cancellationToken);
        if (roles.Count == 0 && !await _guildAccessService.CanManageStaffAsync(id, discordUserId, cancellationToken))
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(roles);
    }

    [HttpPost("{id:guid}/permission-roles")]
    public async Task<ActionResult<GuildPermissionRoleDto>> CreatePermissionRole(
        Guid id,
        [FromBody] CreateGuildPermissionRoleRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        try
        {
            var role = await _guildPermissionRoleService.CreateAsync(id, discordUserId, request, cancellationToken);
            if (role is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }

            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/permission-roles/{roleId:guid}")]
    public async Task<ActionResult<GuildPermissionRoleDto>> UpdatePermissionRole(
        Guid id,
        Guid roleId,
        [FromBody] UpdateGuildPermissionRoleRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        try
        {
            var role = await _guildPermissionRoleService.UpdateAsync(id, roleId, discordUserId, request, cancellationToken);
            if (role is null)
            {
                return NotFound(new { message = "Permission role not found or access denied." });
            }

            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/permission-roles/{roleId:guid}")]
    public async Task<IActionResult> DeletePermissionRole(
        Guid id,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var success = await _guildPermissionRoleService.DeleteAsync(id, roleId, discordUserId, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Permission role not found or access denied." });
        }

        return Ok(new { message = "Permission role removed." });
    }

    [HttpGet("{id:guid}/profile")]
    public async Task<ActionResult<GuildProfileDto>> GetProfile(Guid id, CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var profile = await _guildProfileService.GetProfileAsync(id, discordUserId, cancellationToken);
        if (profile is null)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        return Ok(profile);
    }

    [HttpPut("{id:guid}/profile")]
    public async Task<ActionResult<GuildProfileDto>> UpdateProfile(
        Guid id,
        [FromBody] UpdateGuildProfileRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        try
        {
            var profile = await _guildProfileService.UpdateProfileAsync(id, discordUserId, request, cancellationToken);
            if (profile is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }

            return Ok(profile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}/moderation/permission-roles")]
    public async Task<ActionResult<IReadOnlyList<ModerationPermissionRoleDto>>> GetModerationPermissionRoles(
        Guid id,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var access = await _guildAccessService.GetAccessAsync(id, discordUserId, cancellationToken);
        if (access is null || !access.CanManageSettings)
        {
            return NotFound(new { message = "Guild not found or access denied." });
        }

        var roles = await _moderationPermissionRoleService.GetRolesAsync(id, discordUserId, cancellationToken);
        return Ok(roles);
    }

    [HttpPost("{id:guid}/moderation/permission-roles")]
    public async Task<ActionResult<ModerationPermissionRoleDto>> CreateModerationPermissionRole(
        Guid id,
        [FromBody] CreateModerationPermissionRoleRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        try
        {
            var role = await _moderationPermissionRoleService.CreateAsync(id, discordUserId, request, cancellationToken);
            if (role is null)
            {
                return NotFound(new { message = "Guild not found or access denied." });
            }

            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/moderation/permission-roles/{roleId:guid}")]
    public async Task<ActionResult<ModerationPermissionRoleDto>> UpdateModerationPermissionRole(
        Guid id,
        Guid roleId,
        [FromBody] UpdateModerationPermissionRoleRequest request,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        try
        {
            var role = await _moderationPermissionRoleService.UpdateAsync(id, roleId, discordUserId, request, cancellationToken);
            if (role is null)
            {
                return NotFound(new { message = "Moderation permission role not found or access denied." });
            }

            return Ok(role);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}/moderation/permission-roles/{roleId:guid}")]
    public async Task<IActionResult> DeleteModerationPermissionRole(
        Guid id,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var success = await _moderationPermissionRoleService.DeleteAsync(id, roleId, discordUserId, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = "Moderation permission role not found or access denied." });
        }

        return Ok(new { message = "Moderation permission role removed." });
    }
}
