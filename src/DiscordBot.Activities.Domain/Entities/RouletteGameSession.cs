namespace DiscordBot.Activities.Domain.Entities;

public class RouletteGameSession : ActivitiesEntity
{
    public Guid GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public string HostUserDiscordId { get; set; } = string.Empty;
    public string HostUsername { get; set; } = string.Empty;
    public string Status { get; set; } = "Waiting";
    public int MinPlayers { get; set; } = 2;
    public int MaxPlayers { get; set; } = 6;
    public int WinnerCoins { get; set; }
    public int SecondPlaceCoins { get; set; }
    public int ParticipationCoins { get; set; }
    public int CurrentRound { get; set; }
    public string? CurrentTurnUserDiscordId { get; set; }
    public string? PendingTargetUserDiscordId { get; set; }
    public string PendingActionStatus { get; set; } = "None";
    public DateTimeOffset? PendingActionExpiresAtUtc { get; set; }
    public string? LastSpinResultJson { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public ICollection<RoulettePlayer> Players { get; set; } = [];
    public ICollection<RouletteRound> Rounds { get; set; } = [];
}
