namespace DiscordBot.Activities.Domain.Entities;

public class RouletteBet : ActivitiesEntity
{
    public Guid RouletteRoundId { get; set; }
    public RouletteRound RouletteRound { get; set; } = null!;
    public string DiscordUserId { get; set; } = string.Empty;
    public string BetType { get; set; } = string.Empty;
    public string BetValue { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
    public string Status { get; set; } = "Placed";
    public decimal Payout { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? WalletReservationId { get; set; }
    public DateTimeOffset? SettledAtUtc { get; set; }
}
