namespace DiscordBot.Activities.Domain.Entities;

public class RouletteJoinIntent : ActivitiesEntity
{
    public Guid GameSessionId { get; set; }
    public GameSession GameSession { get; set; } = null!;
    public string DiscordGuildId { get; set; } = string.Empty;
    public string DiscordChannelId { get; set; } = string.Empty;
    public string UserDiscordId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? ConsumedAtUtc { get; set; }
}
