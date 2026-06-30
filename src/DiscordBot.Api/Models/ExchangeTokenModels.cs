namespace DiscordBot.Api.Models;

public sealed class ExchangeTokenRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed class ExchangeTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
}
