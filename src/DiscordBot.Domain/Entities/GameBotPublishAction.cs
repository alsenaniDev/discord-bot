namespace DiscordBot.Domain.Entities;

public class GameBotPublishAction : BaseEntity
{
    public Guid GameEventId { get; set; }
    public GameEvent GameEvent { get; set; } = null!;
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string MessageJson { get; set; } = "{}";
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
