using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class WarningDto
{
    public Guid Id { get; init; }
    public string TargetDiscordUserId { get; init; } = string.Empty;
    public string ModeratorDiscordUserId { get; init; } = string.Empty;
    public string? TargetDisplayName { get; init; }
    public string? ModeratorDisplayName { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ModerationCaseDto
{
    public Guid Id { get; init; }
    public ModerationCaseType Type { get; init; }
    public string? TargetDiscordUserId { get; init; }
    public string ModeratorDiscordUserId { get; init; } = string.Empty;
    public string? TargetDisplayName { get; init; }
    public string? ModeratorDisplayName { get; init; }
    public string? Reason { get; init; }
    public int? MessageCount { get; init; }
    public string? ChannelDiscordId { get; init; }
    public string? ChannelName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateWarningRequest
{
    public required string DiscordGuildId { get; set; }
    public required string TargetDiscordUserId { get; set; }
    public required string ModeratorDiscordUserId { get; set; }
    public required string Reason { get; set; }
    public string? ModeratorDisplayName { get; set; }
    public string? TargetDisplayName { get; set; }
}

public sealed class CreateModerationCaseRequest
{
    public required string DiscordGuildId { get; set; }
    public ModerationCaseType Type { get; set; }
    public string? TargetDiscordUserId { get; set; }
    public required string ModeratorDiscordUserId { get; set; }
    public string? Reason { get; set; }
    public int? MessageCount { get; set; }
    public string? ChannelDiscordId { get; set; }
    public string? ModeratorDisplayName { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
}

public sealed class ModerationListFilter
{
    public string? TargetUserId { get; set; }
    public ModerationCaseType? Type { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}
