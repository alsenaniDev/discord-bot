using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class TicketTimelineEventDto
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public TicketTimelineEventType EventType { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? ActorDiscordUserId { get; init; }
    public string? ActorDisplayName { get; init; }
    public string? Content { get; init; }
    public Guid? RelatedTimelineEventId { get; init; }
    public string? MetadataJson { get; init; }
}

public sealed class RecordTicketMessageSentRequest
{
    public required string ChannelDiscordId { get; set; }
    public required string DiscordMessageId { get; set; }
    public required string AuthorDiscordUserId { get; set; }
    public string? AuthorDisplayName { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
}

public sealed class RecordTicketArchivePostedRequest
{
    public required string ArchiveChannelDiscordId { get; set; }
    public string? ActorDiscordUserId { get; set; }
    public string? ActorDisplayName { get; set; }
}

public sealed class AcknowledgeTicketMessageDeliveryRequest
{
    public bool Delivered { get; set; } = true;
    public string? FailureReason { get; set; }
}

public sealed class TicketTimelinePreviewDto
{
    public IReadOnlyList<string> PreviewLines { get; init; } = [];
}
