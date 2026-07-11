namespace DiscordBot.Activities.Domain.Entities;

public class GamePlayer : ActivitiesEntity
{
    public Guid GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = "Joined";
    public int Position { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LeftAtUtc { get; set; }
}
