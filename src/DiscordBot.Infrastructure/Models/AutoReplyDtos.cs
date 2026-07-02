using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class AutoReplyRuleDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string Trigger { get; init; } = string.Empty;
    public string Response { get; init; } = string.Empty;
    public AutoReplyMatchMode MatchMode { get; init; }
    public AutoReplyScope Scope { get; init; }
    public bool Enabled { get; init; }
    public int Priority { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateAutoReplyRuleRequest
{
    public required string Trigger { get; set; }
    public required string Response { get; set; }
    public AutoReplyMatchMode MatchMode { get; set; } = AutoReplyMatchMode.Contains;
    public AutoReplyScope Scope { get; set; } = AutoReplyScope.AllChannels;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
}

public sealed class UpdateAutoReplyRuleRequest
{
    public required string Trigger { get; set; }
    public required string Response { get; set; }
    public AutoReplyMatchMode MatchMode { get; set; } = AutoReplyMatchMode.Contains;
    public AutoReplyScope Scope { get; set; } = AutoReplyScope.AllChannels;
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
}

public sealed class SendTicketMessageRequest
{
    public required string Content { get; set; }
}

public sealed class TicketOutboundMessageDto
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string SenderDiscordUserId { get; init; } = string.Empty;
    public string? SenderDisplayName { get; init; }
    public bool IsDelivered { get; init; }
    public bool DeliveryFailed { get; init; }
    public Guid StaffReplyQueuedTimelineEventId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? DeliveredAt { get; init; }
}

public sealed class PendingTicketMessageDto
{
    public Guid Id { get; init; }
    public Guid TicketId { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string ChannelDiscordId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? SenderDisplayName { get; init; }
    public string StaffReplyPrefix { get; init; } = string.Empty;
}
