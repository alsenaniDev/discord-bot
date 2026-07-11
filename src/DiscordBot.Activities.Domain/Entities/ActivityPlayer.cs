namespace DiscordBot.Activities.Domain.Entities;

public class ActivityPlayer : ActivitiesEntity
{
    public Guid ActivitySessionId { get; set; }
    public ActivitySession ActivitySession { get; set; } = null!;
    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string ConnectionStatus { get; set; } = "Connected";
    public string? LastConnectionId { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
