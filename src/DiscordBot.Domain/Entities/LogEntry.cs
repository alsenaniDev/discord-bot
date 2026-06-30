using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

/// <summary>
/// Audit trail for bot actions and notable server events.
/// </summary>
public class LogEntry : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public LogEventType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ActorDiscordUserId { get; set; }
    public string? TargetDiscordUserId { get; set; }
    public string? ChannelDiscordId { get; set; }
    public string? MetadataJson { get; set; }
}
