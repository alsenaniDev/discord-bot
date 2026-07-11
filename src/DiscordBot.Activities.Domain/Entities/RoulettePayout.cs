namespace DiscordBot.Activities.Domain.Entities;

public class RoulettePayout : ActivitiesEntity
{
    public Guid RouletteRoundId { get; set; }
    public RouletteRound RouletteRound { get; set; } = null!;
    public string DiscordUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingPayout";
    public int RetryCount { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public DateTimeOffset? NextAttemptAtUtc { get; set; }
    public DateTimeOffset? ProcessingStartedAtUtc { get; set; }
    public string? ProcessingOwner { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? PaidAtUtc { get; set; }
}
