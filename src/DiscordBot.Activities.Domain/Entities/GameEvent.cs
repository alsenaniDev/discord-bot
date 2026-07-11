namespace DiscordBot.Activities.Domain.Entities;

public class GameEvent : ActivitiesEntity
{
    public Guid GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public string GameKey { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string PayloadJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? DiscordUserId { get; set; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
