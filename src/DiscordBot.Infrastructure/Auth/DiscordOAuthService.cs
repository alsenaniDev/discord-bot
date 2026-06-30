using System.Net.Http.Headers;
using System.Net.Http.Json;
using DiscordBot.Infrastructure.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DiscordBot.Infrastructure.Auth;

public class DiscordOAuthService : IDiscordOAuthService
{
    private const string AuthorizeEndpoint = "https://discord.com/api/oauth2/authorize";
    private const string TokenEndpoint = "https://discord.com/api/oauth2/token";
    private const string UserEndpoint = "https://discord.com/api/users/@me";
    private const string OAuthScope = "identify";
    private const string StateCachePrefix = "oauth-state:";

    private readonly HttpClient _httpClient;
    private readonly DiscordOptions _options;
    private readonly IMemoryCache _cache;

    public DiscordOAuthService(
        HttpClient httpClient,
        IOptions<DiscordOptions> options,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
    }

    public (string AuthorizeUrl, string State) CreateLoginRequest()
    {
        ValidateConfiguration();

        var state = Guid.NewGuid().ToString("N");
        _cache.Set(StateCachePrefix + state, true, TimeSpan.FromMinutes(10));

        var authorizeUrl =
            $"{AuthorizeEndpoint}" +
            $"?client_id={Uri.EscapeDataString(_options.ClientId)}" +
            $"&redirect_uri={Uri.EscapeDataString(_options.RedirectUri)}" +
            $"&response_type=code" +
            $"&scope={Uri.EscapeDataString(OAuthScope)}" +
            $"&state={Uri.EscapeDataString(state)}";

        return (authorizeUrl, state);
    }

    public async Task<DiscordProfile> ExchangeCodeAsync(
        string code,
        string state,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        if (!_cache.TryGetValue(StateCachePrefix + state, out _))
        {
            throw new InvalidOperationException("Invalid or expired OAuth state.");
        }

        _cache.Remove(StateCachePrefix + state);

        var tokenResponse = await RequestTokenAsync(code, cancellationToken);
        var discordUser = await RequestUserAsync(tokenResponse.AccessToken, cancellationToken);

        return new DiscordProfile
        {
            DiscordUserId = discordUser.Id,
            Username = discordUser.Username,
            GlobalName = discordUser.GlobalName,
            AvatarUrl = BuildAvatarUrl(discordUser.Id, discordUser.Avatar)
        };
    }

    private async Task<DiscordTokenResponse> RequestTokenAsync(string code, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = _options.RedirectUri
            })
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<DiscordTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Discord returned an empty token response.");

        return token;
    }

    private async Task<DiscordUserResponse> RequestUserAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, UserEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DiscordUserResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Discord returned an empty user profile.");
    }

    private static string? BuildAvatarUrl(string userId, string? avatarHash)
    {
        if (string.IsNullOrWhiteSpace(avatarHash))
        {
            return null;
        }

        return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png";
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId))
            throw new InvalidOperationException("Discord:ClientId is not configured.");

        if (string.IsNullOrWhiteSpace(_options.ClientSecret))
            throw new InvalidOperationException("Discord:ClientSecret is not configured.");

        if (string.IsNullOrWhiteSpace(_options.RedirectUri))
            throw new InvalidOperationException("Discord:RedirectUri is not configured.");
    }
}
