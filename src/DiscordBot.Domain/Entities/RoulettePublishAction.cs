namespace DiscordBot.Domain.Entities;

public class RoulettePublishAction : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;
    public Guid RouletteRoomId { get; set; }
    public RouletteRoom RouletteRoom { get; set; } = null!;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Type { get; set; } = "RoomInvite";
    public string Status { get; set; } = "Pending";
    public string PayloadJson { get; set; } = "{}";
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
