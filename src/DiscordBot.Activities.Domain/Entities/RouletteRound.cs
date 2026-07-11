namespace DiscordBot.Activities.Domain.Entities;

public class RouletteRound : ActivitiesEntity
{
    public Guid RouletteGameSessionId { get; set; }
    public RouletteGameSession RouletteGameSession { get; set; } = null!;
    public int RoundNumber { get; set; }
    public string Status { get; set; } = "Created";
    public string SpinnerUserDiscordId { get; set; } = string.Empty;
    public string? TargetUserDiscordId { get; set; }
    public int? SelectedIndex { get; set; }
    public string ResultJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public ICollection<RouletteBet> Bets { get; set; } = [];
    public ICollection<RoulettePayout> Payouts { get; set; } = [];
}
