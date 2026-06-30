using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class DiscordChannel : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public string DiscordChannelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DiscordChannelType Type { get; set; }
    public int Position { get; set; }
}
