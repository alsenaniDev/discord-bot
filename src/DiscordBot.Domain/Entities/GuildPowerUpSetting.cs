namespace DiscordBot.Domain.Entities;

public class GuildPowerUpSetting : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid GamePowerUpDefinitionId { get; set; }
    public GamePowerUpDefinition GamePowerUpDefinition { get; set; } = null!;
    public bool IsEnabledForGuild { get; set; } = true;
    public int Price { get; set; }
    public int MaxUsesPerGame { get; set; } = 1;
}
