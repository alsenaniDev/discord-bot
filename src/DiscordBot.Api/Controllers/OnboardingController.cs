using System.Security.Claims;
using DiscordBot.Api.Extensions;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/onboarding")]
public class OnboardingController : ControllerBase
{
    private readonly IOnboardingService _onboardingService;

    public OnboardingController(IOnboardingService onboardingService)
    {
        _onboardingService = onboardingService;
    }

    [HttpGet("discord-guilds")]
    public async Task<ActionResult<IReadOnlyList<DiscordGuildOnboardingDto>>> GetDiscordGuilds(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Missing user identity in token." });
        }

        var guilds = await _onboardingService.GetMyDiscordGuildsAsync(userId, cancellationToken);
        return Ok(guilds);
    }

    [HttpGet("status")]
    public async Task<ActionResult<OnboardingStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var discordUserId = User.GetDiscordUserId();
        if (string.IsNullOrWhiteSpace(discordUserId))
        {
            return Unauthorized(new { message = "Missing Discord user identity in token." });
        }

        var status = await _onboardingService.GetStatusAsync(discordUserId, cancellationToken);
        return Ok(status);
    }
}
