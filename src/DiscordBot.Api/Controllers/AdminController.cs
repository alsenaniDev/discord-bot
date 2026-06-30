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

    public AdminController(IAdminService adminService, ISubscriptionService subscriptionService)
    {
        _adminService = adminService;
        _subscriptionService = subscriptionService;
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
}
