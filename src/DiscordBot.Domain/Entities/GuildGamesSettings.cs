namespace DiscordBot.Domain.Entities;

public class GuildGamesSettings : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public string? GamesChannelDiscordId { get; set; }
    public bool AutoPostPanel { get; set; }
    public string? GamesPanelMessageDiscordId { get; set; }
}
