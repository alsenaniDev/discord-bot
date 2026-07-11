using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiscordBot.Activities.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/internal/bot/roulette")]
public class InternalBotRouletteController(IRouletteRuntimeService roulette, IConfiguration configuration, ILogger<InternalBotRouletteController> logger) : ControllerBase
{
    [HttpGet("announcements/pending")]
    public async Task<IActionResult> PendingAnnouncements(CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        return Ok(await roulette.GetPendingAnnouncementsAsync(ct));
    }

    [HttpPost("announcements/{gameSessionId:guid}/ack")]
    public async Task<IActionResult> AckAnnouncement(Guid gameSessionId, AckRouletteAnnouncementRequest request, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        return await roulette.AckAnnouncementAsync(gameSessionId, request, ct) ? Ok() : NotFound();
    }

    [HttpPost("sessions/{gameSessionId:guid}/prepare-join")]
    public async Task<IActionResult> PrepareJoin(Guid gameSessionId, PrepareRouletteJoinRequest request, CancellationToken ct)
    {
        if (!Authorized()) return Unauthorized(new { message = "Invalid activities service key." });
        var result = await roulette.PrepareJoinAsync(gameSessionId, request, ct);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Activities Roulette prepare join denied. GameSessionId={GameSessionId}, GuildId={GuildId}, ChannelId={ChannelId}, UserDiscordId={UserDiscordId}, Reason={Reason}.",
                gameSessionId,
                request.GuildDiscordId,
                request.ChannelDiscordId,
                request.UserDiscordId,
                result.Code ?? result.Error);
        }
        return StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { code = result.Code, message = result.Error });
    }

    private bool Authorized()
    {
        var expected = configuration["ActivitiesDiagnostics:ServiceToken"] ?? configuration["PlatformApi:ServiceToken"];
        return !string.IsNullOrWhiteSpace(expected)
            && Request.Headers.TryGetValue("X-Activities-Service-Key", out var provided)
            && provided == expected;
    }
}
