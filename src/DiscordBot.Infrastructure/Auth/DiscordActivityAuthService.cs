using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DiscordBot.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace DiscordBot.Infrastructure.Auth;

public interface IDiscordActivityAuthService
{
    Task<ActivityTokenResponse?> ExchangeCodeAsync(string code, CancellationToken ct = default);
    Task<ActivityDiscordUser?> ValidateAccessTokenAsync(string accessToken, CancellationToken ct = default);
}

public sealed class DiscordActivityAuthService(HttpClient http, IOptions<DiscordOptions> options) : IDiscordActivityAuthService
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

    private static string? BuildAvatarUrl(string userId, string? avatarHash)
    {
        if (string.IsNullOrWhiteSpace(avatarHash)) return null;
        return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png";
    }
}

public sealed record ActivityTokenResponse(string AccessToken, int ExpiresIn, string TokenType, string Scope);
public sealed record ActivityDiscordUser(string Id, string Username, string? GlobalName, string? AvatarUrl);
