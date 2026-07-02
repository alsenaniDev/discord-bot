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
    public string? ActorDisplayName { get; init; }
    public string? TargetDisplayName { get; init; }
    public string? ChannelName { get; init; }
    public string? RoleDiscordId { get; init; }
    public string? RoleName { get; init; }
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
    public string? ActorDisplayName { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
    public string? RoleDiscordId { get; set; }
    public string? RoleName { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class LogListFilter
{
    public LogEventType? Type { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public string? Search { get; set; }
    public string? UserId { get; set; }
}

public sealed class ClearLogsRequest
{
    public string Confirmation { get; set; } = string.Empty;
}

public sealed class ClearLogsResult
{
    public int DeletedCount { get; init; }
}
