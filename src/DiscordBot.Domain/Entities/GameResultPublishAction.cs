namespace DiscordBot.Domain.Entities;

public class GameResultPublishAction : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Type { get; set; } = "Result";
    public string Status { get; set; } = "Pending";
    public string PayloadJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
