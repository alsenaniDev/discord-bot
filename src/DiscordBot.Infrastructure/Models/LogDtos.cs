using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class LogEntryDto
{
    public Guid Id { get; init; }
    public LogEventType Type { get; init; }
    public string TypeLabel { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? ActorDiscordUserId { get; init; }
    public string? TargetDiscordUserId { get; init; }
    public string? ChannelDiscordId { get; init; }
    public string? MetadataJson { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateLogRequest
{
    public string? DiscordGuildId { get; set; }
    public LogEventType Type { get; set; }
    public required string Message { get; set; }
    public string? ActorDiscordUserId { get; set; }
    public string? TargetDiscordUserId { get; set; }
    public string? ChannelDiscordId { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class LogListFilter
{
    public LogEventType? Type { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Search { get; set; }
}
