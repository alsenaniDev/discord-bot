namespace DiscordBot.Domain.Entities;

public class TicketOutboundMessage : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;

    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public required string Content { get; set; }
    public required string SenderDiscordUserId { get; set; }
    public string? SenderDisplayName { get; set; }
    public bool IsDelivered { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public bool DeliveryFailed { get; set; }
    public string? DeliveryFailureReason { get; set; }

    /// <summary>StaffReplyQueued Timeline Event (D-001 §8, BR-T02).</summary>
    public Guid StaffReplyQueuedTimelineEventId { get; set; }
}
