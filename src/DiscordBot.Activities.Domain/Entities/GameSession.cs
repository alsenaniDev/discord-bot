namespace DiscordBot.Activities.Domain.Entities;

public class GameSession : ActivitiesEntity
{
    public Guid ActivitySessionId { get; set; }
    public ActivitySession ActivitySession { get; set; } = null!;
    public string GameKey { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordChannelId { get; set; } = string.Empty;
    public string Status { get; set; } = "Created";
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ResultJson { get; set; }
    public uint RowVersion { get; set; }
    public ICollection<GamePlayer> Players { get; set; } = [];
    public ICollection<GameEvent> Events { get; set; } = [];
    public RouletteGameSession? Roulette { get; set; }
}
