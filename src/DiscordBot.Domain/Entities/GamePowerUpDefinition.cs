namespace DiscordBot.Domain.Entities;

public class GamePowerUpDefinition : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsEnabledGlobally { get; set; } = true;
    public int DefaultPrice { get; set; }
    public ICollection<GuildPowerUpSetting> GuildSettings { get; set; } = [];
    public ICollection<PlayerPowerUpInventory> Inventories { get; set; } = [];
}
