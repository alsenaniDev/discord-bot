namespace DiscordBot.Domain.Entities;

public class PlayerPowerUpInventory : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public Guid GamePowerUpDefinitionId { get; set; }
    public GamePowerUpDefinition GamePowerUpDefinition { get; set; } = null!;
    public int Quantity { get; set; }
}
