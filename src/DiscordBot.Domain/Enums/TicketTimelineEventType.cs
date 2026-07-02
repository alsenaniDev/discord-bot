namespace DiscordBot.Domain.Enums;

/// <summary>
/// Timeline Event types for the Ticket aggregate (D-001 §8).
/// </summary>
public enum TicketTimelineEventType
{
    TicketCreated = 1,
    MessageSent = 2,
    StaffReplyQueued = 3,
    StaffReplyDelivered = 4,
    StaffReplyFailed = 5,
    StatusChanged = 6,
    ArchivePosted = 7
}
