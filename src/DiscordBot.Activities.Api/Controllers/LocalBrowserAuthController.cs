using DiscordBot.Activities.Api.Options;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DiscordBot.Activities.Api.Controllers;

[AllowAnonymous, ApiController, Route("api/auth/local")]
public sealed class LocalBrowserAuthController(
    IWebHostEnvironment environment,
    IOptions<LocalBrowserModeOptions> options,
    IActivityTokenService tokens,
    ILogger<LocalBrowserAuthController> logger) : ControllerBase
{
    [HttpGet("profiles")]
    public IActionResult Profiles()
    {
        var mode = options.Value;
        if (!Allowed(mode)) return NotFound();
        return Ok(mode.Profiles
            .Where(IsValidProfile)
            .Select(x => new { name = x.Name, username = string.IsNullOrWhiteSpace(x.Username) ? x.Name : x.Username })
            .OrderBy(x => x.name)
            .ToList());
    }

    [HttpPost("exchange")]
    public IActionResult Exchange(ExchangeLocalActivityProfileRequest request)
    {
        var mode = options.Value;
        if (!Allowed(mode)) return NotFound();

        var profileName = request.ProfileName.Trim();
        var profile = mode.Profiles.FirstOrDefault(x => string.Equals(x.Name, profileName, StringComparison.Ordinal));
        if (!IsValidProfile(profile)
            || !ValidSnowflake(mode.GuildDiscordId)
            || !ValidSnowflake(mode.ChannelDiscordId)
            || string.IsNullOrWhiteSpace(mode.ActivityInstanceId))
        {
            logger.LogWarning("Local browser profile auth rejected. ProfileName={ProfileName}, Environment={Environment}, Enabled={Enabled}.", profileName, environment.EnvironmentName, mode.Enabled);
            return BadRequest(new { code = "local_profile_invalid", message = "ملف الاختبار المحلي غير صالح." });
        }

        var token = tokens.CreateToken(new TrustedDiscordUser
        {
            DiscordUserId = profile!.DiscordUserId.Trim(),
            Username = string.IsNullOrWhiteSpace(profile.Username) ? profile.Name.Trim() : profile.Username.Trim(),
            AvatarUrl = profile.AvatarUrl,
            DiscordGuildId = mode.GuildDiscordId.Trim(),
            DiscordChannelId = mode.ChannelDiscordId.Trim(),
            ActivityInstanceId = mode.ActivityInstanceId.Trim()
        });

        logger.LogInformation(
            "Issued local browser Activities token. ProfileName={ProfileName}, DiscordUserId={DiscordUserId}, GuildId={GuildId}, ChannelId={ChannelId}, ActivityInstanceId={ActivityInstanceId}.",
            profile.Name,
            profile.DiscordUserId,
            mode.GuildDiscordId,
            mode.ChannelDiscordId,
            mode.ActivityInstanceId);

        return Ok(new
        {
            token.AccessToken,
            token.ExpiresAt,
            user = token.User,
            guildDiscordId = mode.GuildDiscordId,
            channelDiscordId = mode.ChannelDiscordId,
            activityInstanceId = mode.ActivityInstanceId
        });
    }

    private bool Allowed(LocalBrowserModeOptions mode) => environment.IsDevelopment() && mode.Enabled;

    private static bool IsValidProfile(LocalBrowserProfileOptions? profile) =>
        profile is not null
        && !string.IsNullOrWhiteSpace(profile.Name)
        && ValidSnowflake(profile.DiscordUserId);

    private static bool ValidSnowflake(string value) => ulong.TryParse(value, out var parsed) && parsed > 0;
}
