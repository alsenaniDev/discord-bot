using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Api.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DiscordBot.Activities.Api.Controllers;

[Authorize, ApiController, Route("api/activity-sessions")]
public class ActivitySessionsController(
    IActivitySessionService sessions,
    IOptions<ActivityRuntimeAuthOptions> authOptions,
    IWebHostEnvironment environment,
    ILogger<ActivitySessionsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateActivitySessionRequest request, CancellationToken ct)
    {
        var userId = User.FindFirst("discord_user_id")?.Value;
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(new { message = "انتهت صلاحية تسجيل الدخول. افتح مركز الألعاب مرة ثانية." });
        var trustedGuildId = User.FindFirst("discord_guild_id")?.Value;
        var trustedChannelId = User.FindFirst("discord_channel_id")?.Value;
        var trustedInstanceId = User.FindFirst("activity_instance_id")?.Value;
        if (string.IsNullOrWhiteSpace(trustedGuildId) || string.IsNullOrWhiteSpace(trustedChannelId))
        {
            logger.LogWarning("Activity session create rejected because token is missing trusted guild/channel claims. user={DiscordUserId}, requestedGuild={RequestedGuildId}, requestedChannel={RequestedChannelId}", userId, request.DiscordGuildId, request.DiscordChannelId);
            return StatusCode(403, new { message = "جلسة Discord Activity غير موثوقة. افتح مركز الألعاب مرة ثانية." });
        }

        if (!string.Equals(trustedGuildId, request.DiscordGuildId, StringComparison.Ordinal) || !string.Equals(trustedChannelId, request.DiscordChannelId, StringComparison.Ordinal))
        {
            logger.LogWarning("Activity session create rejected because trusted context mismatched. user={DiscordUserId}, tokenGuild={TokenGuildId}, tokenChannel={TokenChannelId}, requestedGuild={RequestedGuildId}, requestedChannel={RequestedChannelId}", userId, trustedGuildId, trustedChannelId, request.DiscordGuildId, request.DiscordChannelId);
            return StatusCode(403, new { message = "لا يمكن استخدام جلسة الألعاب خارج الروم الذي فُتحت منه." });
        }

        if (string.IsNullOrWhiteSpace(trustedInstanceId))
        {
            if (!environment.IsDevelopment() || !authOptions.Value.AllowMissingActivityInstanceInDevelopment)
            {
                logger.LogWarning("Activity session create rejected because token is missing activity instance. user={DiscordUserId}, guild={GuildId}, channel={ChannelId}", userId, trustedGuildId, trustedChannelId);
                return StatusCode(403, new { message = "جلسة Discord Activity غير مكتملة. افتح مركز الألعاب مرة ثانية." });
            }

            logger.LogWarning("Activity session create allowed without activity instance claim in Development. user={DiscordUserId}, guild={GuildId}, channel={ChannelId}", userId, trustedGuildId, trustedChannelId);
        }

        if (!string.IsNullOrWhiteSpace(request.DiscordActivityInstanceId) && !string.IsNullOrWhiteSpace(trustedInstanceId) && !string.Equals(request.DiscordActivityInstanceId, trustedInstanceId, StringComparison.Ordinal))
        {
            logger.LogWarning("Activity session create rejected because activity instance mismatched. user={DiscordUserId}, tokenInstance={TokenInstanceId}, requestedInstance={RequestedInstanceId}", userId, trustedInstanceId, request.DiscordActivityInstanceId);
            return StatusCode(403, new { message = "لا يمكن استخدام هذه الجلسة من Activity مختلف." });
        }

        request.DiscordActivityInstanceId = trustedInstanceId;
        var user = new TrustedDiscordUser
        {
            DiscordUserId = userId,
            Username = User.FindFirst("username")?.Value ?? userId,
            AvatarUrl = User.FindFirst("avatar_url")?.Value,
            DiscordGuildId = trustedGuildId,
            DiscordChannelId = trustedChannelId,
            ActivityInstanceId = trustedInstanceId
        };
        var result = await sessions.CreateSessionAsync(request, user, ct);
        return StatusCode(result.StatusCode, result.Succeeded ? result.Value : new { message = result.Error });
    }
}
