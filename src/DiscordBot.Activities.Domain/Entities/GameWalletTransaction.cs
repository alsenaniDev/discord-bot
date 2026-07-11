namespace DiscordBot.Activities.Domain.Entities;

public class GameWalletTransaction : ActivitiesEntity
{
    public Guid GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? PlatformReservationId { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ErrorMessage { get; set; }
}
