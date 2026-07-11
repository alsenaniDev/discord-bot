namespace DiscordBot.Activities.Domain.Entities;

public class RoulettePlayer : ActivitiesEntity
{
    public Guid RouletteGameSessionId { get; set; }
    public RouletteGameSession RouletteGameSession { get; set; } = null!;
    public string DiscordUserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsHost { get; set; }
    public bool IsAlive { get; set; } = true;
    public int Position { get; set; }
    public int Eliminations { get; set; }
    public DateTimeOffset JoinedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EliminatedAtUtc { get; set; }
}
