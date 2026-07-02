using DiscordBot.Api.Extensions;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using DiscordBot.Api.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize]
[PlatformAdmin]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IPlanUpgradeRequestService _planUpgradeRequestService;

    public AdminController(
        IAdminService adminService,
        ISubscriptionService subscriptionService,
        IPlanUpgradeRequestService planUpgradeRequestService)
    {
        _adminService = adminService;
        _subscriptionService = subscriptionService;
        _planUpgradeRequestService = planUpgradeRequestService;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<AdminStatsDto>> GetStats(CancellationToken cancellationToken)
    {
        var stats = await _adminService.GetStatsAsync(cancellationToken);
        return Ok(stats);
    }

    [HttpGet("guilds")]
    public async Task<ActionResult<IReadOnlyList<AdminGuildSummaryDto>>> GetGuilds(
        CancellationToken cancellationToken)
    {
        var guilds = await _adminService.GetGuildsAsync(cancellationToken);
        return Ok(guilds);
    }

    [HttpGet("guilds/{id:guid}")]
    public async Task<ActionResult<AdminGuildDetailDto>> GetGuild(Guid id, CancellationToken cancellationToken)
    {
        var guild = await _adminService.GetGuildAsync(id, cancellationToken);
        if (guild is null)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(guild);
    }

    [HttpPut("guilds/{id:guid}/subscription")]
    public async Task<ActionResult<GuildSubscriptionDto>> UpdateGuildSubscription(
        Guid id,
        [FromBody] UpdateGuildSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PlanKey))
        {
            return BadRequest(new { message = "Plan key is required." });
        }

        var subscription = await _subscriptionService.UpdateGuildSubscriptionAsAdminAsync(
            id,
            request.PlanKey,
            cancellationToken);

        if (subscription is null)
        {
            return NotFound(new { message = "Guild or plan not found." });
        }

        return Ok(subscription);
    }

    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers(CancellationToken cancellationToken)
    {
        var users = await _adminService.GetUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("upgrade-requests")]
    public async Task<ActionResult<IReadOnlyList<AdminPlanUpgradeRequestDto>>> GetUpgradeRequests(
        CancellationToken cancellationToken)
    {
        var requests = await _planUpgradeRequestService.GetAllRequestsAsync(cancellationToken);
        return Ok(requests);
    }

    [HttpPost("upgrade-requests/{id:guid}/approve")]
    public async Task<ActionResult<AdminPlanUpgradeRequestDto>> ApproveUpgradeRequest(
        Guid id,
        [FromBody] ReviewPlanUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Missing user identity in token." });
        }

        var result = await _planUpgradeRequestService.ApproveAsync(
            id,
            userId.Value,
            request.AdminNote,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Upgrade request not found or already reviewed." });
        }

        return Ok(result);
    }

    [HttpPost("upgrade-requests/{id:guid}/reject")]
    public async Task<ActionResult<AdminPlanUpgradeRequestDto>> RejectUpgradeRequest(
        Guid id,
        [FromBody] ReviewPlanUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized(new { message = "Missing user identity in token." });
        }

        var result = await _planUpgradeRequestService.RejectAsync(
            id,
            userId.Value,
            request.AdminNote,
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Upgrade request not found or already reviewed." });
        }

        return Ok(result);
    }

    [HttpPost("guilds/{id:guid}/subscription/extend")]
    public async Task<ActionResult<GuildSubscriptionDto>> ExtendGuildSubscription(
        Guid id,
        [FromBody] ExtendGuildSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.ExtendSubscriptionAsync(id, request.Months, cancellationToken);
        if (subscription is null)
        {
            return NotFound(new { message = "Guild not found or extension not allowed." });
        }

        return Ok(subscription);
    }

    [HttpPost("guilds/{id:guid}/subscription/cancel")]
    public async Task<ActionResult<GuildSubscriptionDto>> CancelGuildSubscription(
        Guid id,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.CancelSubscriptionAsync(id, cancellationToken);
        if (subscription is null)
        {
            return NotFound(new { message = "Guild not found." });
        }

        return Ok(subscription);
    }

    [HttpGet("plans")]
    public async Task<ActionResult<IReadOnlyList<AdminSubscriptionPlanDto>>> GetPlans(
        CancellationToken cancellationToken)
    {
        var plans = await _subscriptionService.GetAllPlansForAdminAsync(cancellationToken);
        return Ok(plans);
    }

    [HttpPost("plans")]
    public async Task<ActionResult<AdminSubscriptionPlanDto>> CreatePlan(
        [FromBody] CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _subscriptionService.CreatePlanAsync(request, cancellationToken);
            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("plans/{id:guid}")]
    public async Task<ActionResult<AdminSubscriptionPlanDto>> UpdatePlan(
        Guid id,
        [FromBody] UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var plan = await _subscriptionService.UpdatePlanAsync(id, request, cancellationToken);
            if (plan is null)
            {
                return NotFound(new { message = "Plan not found." });
            }

            return Ok(plan);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("plans/{id:guid}")]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _subscriptionService.DeletePlanAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound(new { message = "Plan not found." });
            }

            return Ok(new { message = "Plan deleted." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
