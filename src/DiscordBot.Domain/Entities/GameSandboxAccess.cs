namespace DiscordBot.Domain.Entities;

public class GameSandboxAccess : BaseEntity
{
    public Guid GameVersionId { get; set; }
    public GameVersion GameVersion { get; set; } = null!;
    public string GuildDiscordId { get; set; } = string.Empty;
    public string? UserDiscordId { get; set; }
}
