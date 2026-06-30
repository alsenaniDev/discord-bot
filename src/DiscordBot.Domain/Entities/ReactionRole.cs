namespace DiscordBot.Domain.Entities;

/// <summary>
/// A button-based role panel posted in a Discord channel.
/// </summary>
public class ReactionRole : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public string ChannelDiscordId { get; set; } = string.Empty;
    public string MessageDiscordId { get; set; } = string.Empty;
    public string RoleDiscordId { get; set; } = string.Empty;
    public string ButtonCustomId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ButtonLabel { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
