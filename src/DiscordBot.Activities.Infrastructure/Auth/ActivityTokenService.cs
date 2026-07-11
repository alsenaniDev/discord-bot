using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DiscordBot.Activities.Infrastructure.Auth;

public class ActivityTokenService(IOptions<ActivitiesJwtOptions> options) : IActivityTokenService
{
    public ActivityAuthResponse CreateToken(TrustedDiscordUser user)
    {
        var jwt = options.Value;
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be configured and at least 32 characters.");
        }

        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(Math.Clamp(jwt.AccessTokenMinutes, 5, 120));
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.DiscordUserId),
            new("discord_user_id", user.DiscordUserId),
            new("username", user.Username)
        };
        if (!string.IsNullOrWhiteSpace(user.AvatarUrl)) claims.Add(new Claim("avatar_url", user.AvatarUrl));
        if (!string.IsNullOrWhiteSpace(user.DiscordGuildId)) claims.Add(new Claim("discord_guild_id", user.DiscordGuildId));
        if (!string.IsNullOrWhiteSpace(user.DiscordChannelId)) claims.Add(new Claim("discord_channel_id", user.DiscordChannelId));
        if (!string.IsNullOrWhiteSpace(user.ActivityInstanceId)) claims.Add(new Claim("activity_instance_id", user.ActivityInstanceId));

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), SecurityAlgorithms.HmacSha256));

        return new ActivityAuthResponse
        {
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires,
            User = new ActivityUserDto { DiscordUserId = user.DiscordUserId, Username = user.Username, AvatarUrl = user.AvatarUrl }
        };
    }
}
