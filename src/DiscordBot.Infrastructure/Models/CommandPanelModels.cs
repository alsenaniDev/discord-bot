namespace DiscordBot.Infrastructure.Models;

using DiscordBot.Domain.Enums;

public sealed class CommandPanelButtonDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Style { get; set; } = "Secondary";
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
}

public sealed class CommandPanelConfigDto
{
    public bool Enabled { get; init; }
    public string? ChannelId { get; init; }
    public string? MessageId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public IReadOnlyList<CommandPanelButtonDefinition> Buttons { get; init; } = [];
}

public sealed class CommandPanelRefreshDto
{
    public Guid PanelId { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string ChannelDiscordId { get; init; } = string.Empty;
    public string? MessageDiscordId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public IReadOnlyList<GuildPanelButtonDto> Buttons { get; init; } = [];
}

public sealed class AckCommandPanelRequest
{
    public string? MessageDiscordId { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
}

public sealed class GuildPanelDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string ChannelDiscordId { get; init; } = string.Empty;
    public string? MessageDiscordId { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsPublished { get; init; }
    public bool RefreshRequested { get; init; }
    public PanelPublishStatus PublishStatus { get; init; }
    public DateTimeOffset? LastPublishedAtUtc { get; init; }
    public bool LastPublishFailed { get; init; }
    public string? LastPublishFailureReason { get; init; }
    public DateTimeOffset? LastPublishAttemptedAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
    public IReadOnlyList<GuildPanelButtonDto> Buttons { get; init; } = [];
}

public sealed class GuildPanelButtonDto
{
    public Guid Id { get; init; }
    public string Label { get; init; } = string.Empty;
    public string? Emoji { get; init; }
    public PanelButtonStyle Style { get; init; }
    public PanelButtonActionType ActionType { get; init; }
    public Guid? TicketTypeId { get; init; }
    public string? Url { get; init; }
    public string? ResponseMessage { get; init; }
    public string? RoleDiscordId { get; init; }
    public int SortOrder { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed class SaveGuildPanelRequest
{
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string ChannelDiscordId { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public List<SaveGuildPanelButtonRequest> Buttons { get; set; } = [];
}

public sealed class SaveGuildPanelButtonRequest
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? Emoji { get; set; }
    public PanelButtonStyle Style { get; set; }
    public PanelButtonActionType ActionType { get; set; }
    public Guid? TicketTypeId { get; set; }
    public string? Url { get; set; }
    public string? ResponseMessage { get; set; }
    public string? RoleDiscordId { get; set; }
    public int SortOrder { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public sealed class PanelButtonActionDto
{
    public string DiscordGuildId { get; init; } = string.Empty;
    public Guid PanelId { get; init; }
    public Guid ButtonId { get; init; }
    public PanelButtonActionType ActionType { get; init; }
    public Guid? TicketTypeId { get; init; }
    public string? Url { get; init; }
    public string? ResponseMessage { get; init; }
    public string? RoleDiscordId { get; init; }
}

public sealed class TicketChannelCleanupDto
{
    public Guid TicketId { get; init; }
    public Guid GuildId { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string ChannelDiscordId { get; init; } = string.Empty;
    public int TicketNumber { get; init; }
    public string OwnerDiscordUserId { get; init; } = string.Empty;
    public string? OwnerDisplayName { get; init; }
    public string? ClosedByDiscordUserId { get; init; }
    public string? ClosedByDisplayName { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public string? TicketArchiveChannelId { get; init; }
    public string TicketClosedFromDashboardMessage { get; init; } = string.Empty;
}
