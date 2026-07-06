namespace DiscordBot.Domain.Entities;

public class RouletteJoinIntent : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid RouletteRoomId { get; set; }
    public RouletteRoom RouletteRoom { get; set; } = null!;
    public string UserDiscordId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}
