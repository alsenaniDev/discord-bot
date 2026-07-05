namespace DiscordBot.Domain.Entities;

public class GameSession : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid PlatformGameDefinitionId { get; set; }
    public PlatformGameDefinition PlatformGameDefinition { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public string? ChannelDiscordId { get; set; }
    public string? Username { get; set; }
    public string Status { get; set; } = "Started";
    public int? Score { get; set; }
    public bool? Won { get; set; }
    public int PointsAwarded { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public ICollection<GameResultPublishAction> PublishActions { get; set; } = [];
}
