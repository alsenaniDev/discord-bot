using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

/// <summary>
/// Ticket Summary Read Model (AR-001 §3) — list/triage projection.
/// </summary>
public sealed class TicketSummaryReadModel
{
    public Guid TicketId { get; init; }
    public Guid GuildId { get; init; }
    public int TicketNumber { get; init; }
    public string OwnerDiscordId { get; init; } = string.Empty;
    public string? OwnerUsername { get; init; }
    public TicketStatus Status { get; init; }
    public string DiscordChannelId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public DateTimeOffset LastActivityAt { get; init; }
    public string? LastMessagePreview { get; init; }
    public int MessageCount { get; init; }
    public int StaffReplyCount { get; init; }
    public int FailedDeliveryCount { get; init; }
}

public sealed class PaginatedTicketSummaryReadModel
{
    public IReadOnlyList<TicketSummaryReadModel> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}

public sealed class TicketSummaryQuery
{
    public TicketStatus? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    /// <summary>lastActivity (default), created, number</summary>
    public string Sort { get; set; } = "lastActivity";
}

public enum TicketConversationActorType
{
    System,
    Owner,
    Staff,
    Bot
}

public enum TicketDeliveryStatus
{
    None,
    Queued,
    Delivered,
    Failed
}

/// <summary>
/// Ticket Conversation Read Model entry (AR-001 §3) — presentation projection over Timeline.
/// </summary>
public sealed class TicketConversationEntryReadModel
{
    public Guid EventId { get; init; }
    public Guid TicketId { get; init; }
    public TicketTimelineEventType EventType { get; init; }
    public TicketConversationActorType ActorType { get; init; }
    public string? ActorDiscordId { get; init; }
    public string? ActorUsername { get; init; }
    public string? Content { get; init; }
    public bool IsInternal { get; init; }
    public TicketDeliveryStatus DeliveryStatus { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class PaginatedTicketConversationReadModel
{
    public IReadOnlyList<TicketConversationEntryReadModel> Items { get; init; } = [];
    public bool HasMore { get; init; }
    public DateTimeOffset? NextCursorOccurredAt { get; init; }
    public Guid? NextCursorEventId { get; init; }
}

public sealed class TicketConversationQuery
{
    public DateTimeOffset? CursorOccurredAt { get; set; }
    public Guid? CursorEventId { get; set; }
    public int Limit { get; set; } = 50;
}

/// <summary>
/// Ticket Transcript Read Model metadata (AR-001, CM-004) — durable record header derived from Timeline.
/// </summary>
public sealed class TicketTranscriptMetadataReadModel
{
    public Guid TicketId { get; init; }
    public Guid GuildId { get; init; }
    public int TicketNumber { get; init; }
    public string OwnerDiscordId { get; init; } = string.Empty;
    public string? OwnerUsername { get; init; }
    public TicketStatus Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    /// <summary>Always Timeline — transcript is reconstructed from append-only Timeline events.</summary>
    public string Source { get; init; } = "Timeline";
    /// <summary>Discord archive channel posts a digest only; full record lives here.</summary>
    public bool DiscordArchiveIsDigestOnly { get; init; } = true;
}

/// <summary>
/// Ticket Transcript Read Model (AR-001, CM-004) — full durable record from Timeline / Conversation projection.
/// </summary>
public sealed class TicketTranscriptReadModel
{
    public TicketTranscriptMetadataReadModel Metadata { get; init; } = new();
    public IReadOnlyList<TicketConversationEntryReadModel> Entries { get; init; } = [];
    public bool HasMore { get; init; }
    public DateTimeOffset? NextCursorOccurredAt { get; init; }
    public Guid? NextCursorEventId { get; init; }
}

public sealed class TicketTranscriptQuery
{
    public DateTimeOffset? CursorOccurredAt { get; set; }
    public Guid? CursorEventId { get; set; }
    public int Limit { get; set; } = 50;
}
