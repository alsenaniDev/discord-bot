namespace DiscordBot.Domain.Entities;

public class RouletteRoom : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid PlatformGameDefinitionId { get; set; }
    public PlatformGameDefinition PlatformGameDefinition { get; set; } = null!;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string HostUserDiscordId { get; set; } = string.Empty;
    public string HostUsername { get; set; } = string.Empty;
    public string Status { get; set; } = "Waiting";
    public int MinPlayers { get; set; }
    public int MaxPlayers { get; set; }
    public int WinnerCoins { get; set; }
    public int SecondPlaceCoins { get; set; }
    public int ParticipationCoins { get; set; }
    public int CurrentRound { get; set; }
    public string? CurrentTurnUserDiscordId { get; set; }
    public string? PendingTargetUserDiscordId { get; set; }
    public string? PendingActionStatus { get; set; }
    public DateTimeOffset? PendingActionExpiresAt { get; set; }
    public string? LastSpinResultJson { get; set; }
    public string? InviteMessageDiscordId { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<RouletteRoomPlayer> Players { get; set; } = [];
    public ICollection<RouletteRoundAction> Actions { get; set; } = [];
    public ICollection<RouletteJoinIntent> JoinIntents { get; set; } = [];
    public ICollection<RoulettePublishAction> PublishActions { get; set; } = [];
    public ICollection<RoulettePowerUpUsage> PowerUpUsages { get; set; } = [];
}
