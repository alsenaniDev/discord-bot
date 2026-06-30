namespace DiscordBot.Infrastructure.Auth;

public interface IDiscordOAuthService
{
    /// <summary>Builds the Discord authorize URL and stores CSRF state in memory.</summary>
    (string AuthorizeUrl, string State) CreateLoginRequest();

    /// <summary>Validates state, exchanges code for token, fetches Discord profile.</summary>
    Task<DiscordProfile> ExchangeCodeAsync(string code, string state, CancellationToken cancellationToken = default);
}
