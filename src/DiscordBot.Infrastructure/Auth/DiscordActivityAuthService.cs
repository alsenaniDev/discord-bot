using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DiscordBot.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DiscordBot.Infrastructure.Auth;

public interface IDiscordActivityAuthService
{
    Task<ActivityTokenResponse?> ExchangeCodeAsync(string code, CancellationToken ct = default);
    Task<ActivityDiscordUser?> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default);
}

public sealed class DiscordActivityAuthService(
    HttpClient http,
    IOptions<DiscordOptions> options,
    IOptions<LocalBrowserModeOptions> localBrowserOptions,
    IHostEnvironment environment,
    ILogger<DiscordActivityAuthService> logger) : IDiscordActivityAuthService
{
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
    private const string UserEndpoint = "https://discord.com/api/v10/users/@me";

    public async Task<ActivityTokenResponse?> ExchangeCodeAsync(string code, CancellationToken ct = default)
    {
        var discord = options.Value;
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(discord.ClientId) || string.IsNullOrWhiteSpace(discord.ClientSecret)) return null;
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = discord.ClientId,
                ["client_secret"] = discord.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code.Trim()
            })
        };
        using var response = await http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;
        var token = await response.Content.ReadFromJsonAsync<DiscordTokenResponse>(cancellationToken: ct);
        return token is null || string.IsNullOrWhiteSpace(token.AccessToken) ? null : new ActivityTokenResponse(token.AccessToken, token.ExpiresIn, token.TokenType, token.Scope);
    }

    public async Task<ActivityDiscordUser?> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accessToken)) return null;
        var localUser = TryValidateLocalBrowserToken(accessToken);
        if (localUser is not null) return localUser;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, UserEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await http.SendAsync(request, ct);
            if (response.StatusCode == HttpStatusCode.Unauthorized || !response.IsSuccessStatusCode) return null;
            var user = await response.Content.ReadFromJsonAsync<DiscordUserResponse>(cancellationToken: ct);
            return user is null || string.IsNullOrWhiteSpace(user.Id) ? null : new ActivityDiscordUser(user.Id, user.Username, user.GlobalName, BuildAvatarUrl(user.Id, user.Avatar));
        }
        catch (HttpRequestException) { return null; }
    }

    private ActivityDiscordUser? TryValidateLocalBrowserToken(string accessToken)
    {
        var mode = localBrowserOptions.Value;
        if (!environment.IsDevelopment() || !mode.Enabled) return null;
        if (!new JwtSecurityTokenHandler().CanReadToken(accessToken)) return null;

        if (string.IsNullOrWhiteSpace(mode.ActivitiesJwt.SigningKey)
            || mode.ActivitiesJwt.SigningKey.Length < 32
            || string.IsNullOrWhiteSpace(mode.ActivitiesJwt.Issuer)
            || string.IsNullOrWhiteSpace(mode.ActivitiesJwt.Audience))
        {
            logger.LogWarning("Local browser mode token validation skipped because LocalBrowserMode:ActivitiesJwt is incomplete.");
            return null;
        }

        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = mode.ActivitiesJwt.Issuer,
                ValidAudience = mode.ActivitiesJwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(mode.ActivitiesJwt.SigningKey)),
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            var userId = Claim(principal, "discord_user_id") ?? Claim(principal, JwtRegisteredClaimNames.Sub);
            var guildId = Claim(principal, "discord_guild_id");
            var channelId = Claim(principal, "discord_channel_id");
            var activityInstanceId = Claim(principal, "activity_instance_id");
            var profile = mode.Profiles.FirstOrDefault(x => string.Equals(x.DiscordUserId?.Trim(), userId, StringComparison.Ordinal));

            if (profile is null
                || !string.Equals(guildId, mode.GuildDiscordId?.Trim(), StringComparison.Ordinal)
                || !string.Equals(channelId, mode.ChannelDiscordId?.Trim(), StringComparison.Ordinal)
                || !string.Equals(activityInstanceId, mode.ActivityInstanceId?.Trim(), StringComparison.Ordinal))
            {
                logger.LogWarning(
                    "Local browser mode token rejected. UserId={UserId}, GuildId={GuildId}, ChannelId={ChannelId}, ActivityInstanceId={ActivityInstanceId}, ProfileMatched={ProfileMatched}.",
                    userId,
                    guildId,
                    channelId,
                    activityInstanceId,
                    profile is not null);
                return null;
            }

            var username = Claim(principal, "username") ?? profile.Username ?? profile.Name;
            var avatarUrl = Claim(principal, "avatar_url") ?? profile.AvatarUrl;
            logger.LogDebug("Local browser mode token accepted for user {UserId}, guild {GuildId}, channel {ChannelId}.", userId, guildId, channelId);
            return new ActivityDiscordUser(userId!, username, username, avatarUrl);
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "Local browser mode token validation failed.");
            return null;
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Local browser mode token validation failed.");
            return null;
        }
    }

    private static string? Claim(ClaimsPrincipal principal, string type) => principal.FindFirst(type)?.Value;

    private static string? BuildAvatarUrl(string userId, string? avatarHash)
    {
        if (string.IsNullOrWhiteSpace(avatarHash)) return null;
        return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png";
    }
}

public sealed record ActivityTokenResponse(string AccessToken, int ExpiresIn, string TokenType, string Scope);
public sealed record ActivityDiscordUser(string Id, string Username, string? GlobalName, string? AvatarUrl);
