namespace DiscordBot.Domain.Entities;

public class GameEvent : BaseEntity
{
    public string GameKey { get; set; } = string.Empty;
    public Guid? GameVersionId { get; set; }
    public GameVersion? GameVersion { get; set; }
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string GuildDiscordId { get; set; } = string.Empty;
    public string? ChannelDiscordId { get; set; }
    public string? UserDiscordId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string PayloadJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public ICollection<GameBotPublishAction> BotPublishActions { get; set; } = [];
}
