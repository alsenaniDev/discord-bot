namespace DiscordBot.Activities.Application.Models;

public sealed class CreateActivitySessionRequest
{
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordChannelId { get; set; } = string.Empty;
    public string? DiscordActivityInstanceId { get; set; }
    public string GameKey { get; set; } = string.Empty;
}

public sealed class ActivitySessionDto
{
    public Guid Id { get; set; }
    public string DiscordUserId { get; set; } = string.Empty;
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordChannelId { get; set; } = string.Empty;
    public string GameKey { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public string Mode { get; set; } = "Production";
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
