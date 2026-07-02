using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class Ticket : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public int TicketNumber { get; set; }
    public string OwnerDiscordUserId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public DateTimeOffset? ClosedAt { get; set; }
    public bool ChannelCleanupRequested { get; set; }

    public ICollection<TicketTimelineEvent> TimelineEvents { get; set; } = [];
}
