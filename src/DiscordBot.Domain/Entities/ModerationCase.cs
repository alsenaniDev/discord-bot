using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class ModerationCase : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public ModerationCaseType Type { get; set; }
    public string? TargetDiscordUserId { get; set; }
    public string ModeratorDiscordUserId { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public int? MessageCount { get; set; }
    public string? ChannelDiscordId { get; set; }
}
