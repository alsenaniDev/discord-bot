using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DiscordBot.Activities.Application.Abstractions;
using DiscordBot.Activities.Application.Models;
using DiscordBot.Activities.Infrastructure.Options;
using DiscordBot.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiscordBot.Activities.Infrastructure.Auth;

public class DiscordActivityAuthService(HttpClient http, IOptions<DiscordActivityOptions> options, IActivityTokenService tokens, ILogger<DiscordActivityAuthService> logger) : IActivityAuthService
{
    public async Task<OperationResult<ActivityAuthResponse>> ExchangeDiscordCodeAsync(ExchangeDiscordCodeRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code;
        if (string.IsNullOrWhiteSpace(code)) return OperationResult<ActivityAuthResponse>.Fail("رمز Discord مطلوب.");
        var discord = options.Value;
        if (string.IsNullOrWhiteSpace(discord.ClientId) || string.IsNullOrWhiteSpace(discord.ClientSecret))
            return OperationResult<ActivityAuthResponse>.Fail("إعدادات Discord Activity غير مكتملة.", 500);

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = discord.ClientId,
            ["client_secret"] = discord.ClientSecret,
            ["grant_type"] = "authorization_code",
            ["code"] = code.Trim(),
            ["redirect_uri"] = discord.RedirectUri
        });

        using var tokenResponse = await http.PostAsync("https://discord.com/api/oauth2/token", form, cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Discord Activity code exchange failed with status {StatusCode}.", tokenResponse.StatusCode);
            return OperationResult<ActivityAuthResponse>.Fail("تعذر تسجيل الدخول إلى Discord.", 401);
        }

        var token = await tokenResponse.Content.ReadFromJsonAsync<DiscordOAuthTokenResponse>(cancellationToken: cancellationToken);
        if (token is null || string.IsNullOrWhiteSpace(token.AccessToken)) return OperationResult<ActivityAuthResponse>.Fail("تعذر قراءة رمز Discord.", 401);
        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://discord.com/api/users/@me");
        userRequest.Headers.Authorization = new("Bearer", token.AccessToken);
        using var userResponse = await http.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode) return OperationResult<ActivityAuthResponse>.Fail("تعذر التحقق من مستخدم Discord.", 401);
        var user = await userResponse.Content.ReadFromJsonAsync<DiscordUserResponse>(cancellationToken: cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.Id)) return OperationResult<ActivityAuthResponse>.Fail("تعذر قراءة بيانات مستخدم Discord.", 401);
        var auth = tokens.CreateToken(new TrustedDiscordUser
        {
            DiscordUserId = user.Id,
            Username = user.GlobalName ?? user.Username,
            AvatarUrl = BuildAvatarUrl(user.Id, user.Avatar),
            DiscordGuildId = ValidSnowflake(request.GuildDiscordId) ? request.GuildDiscordId : null,
            DiscordChannelId = ValidSnowflake(request.ChannelDiscordId) ? request.ChannelDiscordId : null,
            ActivityInstanceId = LimitNullable(request.ActivityInstanceId, 128)
        });
        auth.DiscordAccessToken = token.AccessToken;
        auth.DiscordExpiresIn = token.ExpiresIn;
        auth.DiscordTokenType = token.TokenType;
        auth.DiscordScope = token.Scope;
        return OperationResult<ActivityAuthResponse>.Ok(auth);
    }

    private static string? BuildAvatarUrl(string userId, string? avatarHash) => string.IsNullOrWhiteSpace(avatarHash) ? null : $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png";
    private static bool ValidSnowflake(string? value) => !string.IsNullOrWhiteSpace(value) && ulong.TryParse(value, out _);
    private static string? LimitNullable(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
    private sealed class DiscordOAuthTokenResponse { [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty; [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; } [JsonPropertyName("token_type")] public string TokenType { get; set; } = "Bearer"; [JsonPropertyName("scope")] public string Scope { get; set; } = string.Empty; }
    private sealed class DiscordUserResponse { [JsonPropertyName("id")] public string Id { get; set; } = string.Empty; [JsonPropertyName("username")] public string Username { get; set; } = string.Empty; [JsonPropertyName("global_name")] public string? GlobalName { get; set; } [JsonPropertyName("avatar")] public string? Avatar { get; set; } }
}
