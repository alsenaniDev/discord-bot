namespace DiscordBot.Domain.Entities;

/// <summary>
/// Platform feature module (catalog entry).
/// </summary>
public class Module : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<GuildModule> GuildModules { get; set; } = [];
}
