namespace DiscordBot.Activities.Application.Models;

public sealed class ExchangeDiscordCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string? GuildDiscordId { get; set; }
    public string? ChannelDiscordId { get; set; }
    public string? ActivityInstanceId { get; set; }
}

public sealed class ActivityAuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? DiscordAccessToken { get; set; }
    public int DiscordExpiresIn { get; set; }
    public string? DiscordTokenType { get; set; }
    public string? DiscordScope { get; set; }
    public ActivityUserDto User { get; set; } = new();
}

public sealed class ActivityUserDto
{
    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public sealed class TrustedDiscordUser
{
    public string DiscordUserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public string? DiscordGuildId { get; init; }
    public string? DiscordChannelId { get; init; }
    public string? ActivityInstanceId { get; init; }
}
