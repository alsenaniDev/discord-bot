namespace DiscordBot.Domain.Entities;

public class WalletReservation : BaseEntity
{
    public string ReservationId { get; set; } = Guid.NewGuid().ToString("N");
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string DiscordUserId { get; set; } = string.Empty;
    public string GameKey { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "coins";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? CommittedAtUtc { get; set; }
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
