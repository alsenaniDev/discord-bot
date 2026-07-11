namespace DiscordBot.Activities.Domain.Entities;

public class ActivitySession : ActivitiesEntity
{
    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordChannelId { get; set; } = string.Empty;
    public string? DiscordActivityInstanceId { get; set; }
    public string GameKey { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public Guid? PlatformGameVersionId { get; set; }
    public string Mode { get; set; } = "Production";
    public string Status { get; set; } = "Active";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<ActivityPlayer> Players { get; set; } = [];
}
