using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

/// <summary>
/// One append-only fact on the Ticket Timeline (D-001 §3, §8). BR-T03: immutable after creation.
/// </summary>
public class TicketTimelineEvent : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public TicketTimelineEventType EventType { get; set; }

    /// <summary>Business timestamp for ordering (BR-T04).</summary>
    public DateTimeOffset OccurredAt { get; set; }

    public string? ActorDiscordUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? Content { get; set; }
    public string? DiscordMessageId { get; set; }
    public Guid? RelatedTimelineEventId { get; set; }
    public string? MetadataJson { get; set; }
}
