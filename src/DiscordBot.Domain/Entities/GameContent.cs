namespace DiscordBot.Domain.Entities;

public class GameContent : BaseEntity
{
    public Guid? GuildId { get; set; }
    public Guild? Guild { get; set; }
    public Guid PlatformGameDefinitionId { get; set; }
    public PlatformGameDefinition PlatformGameDefinition { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string DataJson { get; set; } = "{}";
    public bool IsEnabled { get; set; } = true;
}
