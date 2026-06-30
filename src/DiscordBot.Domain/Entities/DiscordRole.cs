namespace DiscordBot.Domain.Entities;

public class DiscordRole : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public string DiscordRoleId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? Color { get; set; }
    public int Position { get; set; }
    public bool IsManaged { get; set; }
}
