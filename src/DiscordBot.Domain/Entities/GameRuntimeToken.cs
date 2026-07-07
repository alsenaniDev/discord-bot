namespace DiscordBot.Domain.Entities;

public class GameRuntimeToken : BaseEntity
{
    public string TokenHash { get; set; } = string.Empty;
    public string GameKey { get; set; } = string.Empty;
    public Guid GameVersionId { get; set; }
    public GameVersion GameVersion { get; set; } = null!;
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string GuildDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string UserDiscordId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string Mode { get; set; } = "Production";
    public DateTimeOffset? RevokedAt { get; set; }
}
