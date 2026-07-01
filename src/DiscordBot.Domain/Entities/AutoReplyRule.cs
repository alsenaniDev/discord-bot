using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class AutoReplyRule : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public required string Trigger { get; set; }
    public required string Response { get; set; }
    public AutoReplyMatchMode MatchMode { get; set; } = AutoReplyMatchMode.Contains;
    public AutoReplyScope Scope { get; set; } = AutoReplyScope.AllChannels;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
}
