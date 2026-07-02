using DiscordBot.Domain.Enums;

namespace DiscordBot.Bot.Api.Models;

public sealed class RegisterGuildRequest
{
    public required string DiscordGuildId { get; set; }
    public required string Name { get; set; }
    public required string OwnerDiscordUserId { get; set; }
    public string? IconUrl { get; set; }
}

public sealed class RegisterGuildResponse
{
    public Guid Id { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; set; }
}

public sealed class GuildSettingsResponse
{
    public Guid GuildId { get; set; }
    public bool WelcomeEnabled { get; set; }
    public string? WelcomeChannelId { get; set; }
    public string WelcomeMessage { get; set; } = string.Empty;
    public bool AutoRoleEnabled { get; set; }
    public string? AutoRoleId { get; set; }
    public bool LogsEnabled { get; set; }
    public string? LogChannelId { get; set; }
    public bool TicketsEnabled { get; set; }
    public string? TicketCategoryId { get; set; }
    public string? TicketArchiveChannelId { get; set; }
    public string TicketWelcomeTitle { get; set; } = string.Empty;
    public string TicketWelcomeMessage { get; set; } = string.Empty;
    public string TicketClosedMessage { get; set; } = string.Empty;
    public string TicketClosedFromDashboardMessage { get; set; } = string.Empty;
    public string TicketStaffReplyPrefix { get; set; } = string.Empty;
}

public sealed class GuildModuleStatusResponse
{
    public string Key { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool AllowedByPlan { get; set; }
}

public sealed class CreateLogApiRequest
{
    public required string DiscordGuildId { get; set; }
    public required string Type { get; set; }
    public required string Message { get; set; }
    public string? ActorDiscordUserId { get; set; }
    public string? TargetDiscordUserId { get; set; }
    public string? ChannelDiscordId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class TicketResponse
{
    public Guid Id { get; set; }
    public Guid GuildId { get; set; }
    public int TicketNumber { get; set; }
    public string OwnerDiscordUserId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class CreateTicketApiRequest
{
    public required string DiscordGuildId { get; set; }
    public required string OwnerDiscordUserId { get; set; }
    public required string ChannelDiscordId { get; set; }
    public string? OwnerDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
}

public sealed class CloseTicketApiRequest
{
    public string? ClosedByDiscordUserId { get; set; }
    public string? ClosedByDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
}

public sealed class SetupTicketsApiRequest
{
    public required string TicketCategoryId { get; set; }
}

public enum SyncChannelType
{
    Text = 0,
    Category = 1,
    Voice = 2
}

public sealed class SyncResourcesApiRequest
{
    public List<SyncChannelApiItem> Channels { get; set; } = [];
    public List<SyncRoleApiItem> Roles { get; set; } = [];
    public List<SyncMemberApiItem> Members { get; set; } = [];
}

public sealed class SyncChannelApiItem
{
    public required string DiscordChannelId { get; set; }
    public required string Name { get; set; }
    public SyncChannelType Type { get; set; }
    public int Position { get; set; }
}

public sealed class SyncRoleApiItem
{
    public required string DiscordRoleId { get; set; }
    public required string Name { get; set; }
    public int? Color { get; set; }
    public int Position { get; set; }
    public bool IsManaged { get; set; }
}

public sealed class SyncMemberApiItem
{
    public required string DiscordUserId { get; set; }
    public required string Username { get; set; }
    public string? GlobalName { get; set; }
    public string? Nickname { get; set; }
    public List<string> DiscordRoleIds { get; set; } = [];
}

public sealed class CreateWarningApiRequest
{
    public required string DiscordGuildId { get; set; }
    public required string TargetDiscordUserId { get; set; }
    public required string ModeratorDiscordUserId { get; set; }
    public required string Reason { get; set; }
    public string? ModeratorDisplayName { get; set; }
    public string? TargetDisplayName { get; set; }
}

public sealed class CreateModerationCaseApiRequest
{
    public required string DiscordGuildId { get; set; }
    public int Type { get; set; }
    public string? TargetDiscordUserId { get; set; }
    public required string ModeratorDiscordUserId { get; set; }
    public string? Reason { get; set; }
    public int? MessageCount { get; set; }
    public string? ChannelDiscordId { get; set; }
    public string? ModeratorDisplayName { get; set; }
    public string? TargetDisplayName { get; set; }
    public string? ChannelDisplayName { get; set; }
}

public sealed class WarningApiResponse
{
    public Guid Id { get; set; }
    public string TargetDiscordUserId { get; set; } = string.Empty;
    public string ModeratorDiscordUserId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateReactionRoleApiRequest
{
    public required string DiscordGuildId { get; set; }
    public required string ChannelDiscordId { get; set; }
    public required string MessageDiscordId { get; set; }
    public required string RoleDiscordId { get; set; }
    public required string ButtonCustomId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string ButtonLabel { get; set; }
    public string? CreatedByDiscordUserId { get; set; }
}

public sealed class ReactionRoleApiResponse
{
    public Guid Id { get; set; }
    public Guid GuildId { get; set; }
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string MessageDiscordId { get; set; } = string.Empty;
    public string RoleDiscordId { get; set; } = string.Empty;
    public string ButtonCustomId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ButtonLabel { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CommandPanelRefreshApiResponse
{
    public string DiscordGuildId { get; set; } = string.Empty;
    public CommandPanelConfigApiResponse Config { get; set; } = new();
}

public sealed class CommandPanelConfigApiResponse
{
    public bool Enabled { get; set; }
    public string? ChannelId { get; set; }
    public string? MessageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public List<CommandPanelButtonApiResponse> Buttons { get; set; } = [];
}

public sealed class CommandPanelButtonApiResponse
{
    public string Id { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Style { get; set; } = "Secondary";
    public bool Enabled { get; set; } = true;
    public int Order { get; set; }
}

public sealed class AckCommandPanelApiRequest
{
    public string? MessageId { get; set; }
}

public sealed class TicketCleanupApiResponse
{
    public Guid TicketId { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public int TicketNumber { get; set; }
    public string OwnerDiscordUserId { get; set; } = string.Empty;
    public string? OwnerDisplayName { get; set; }
    public string? ClosedByDiscordUserId { get; set; }
    public string? ClosedByDisplayName { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public string? TicketArchiveChannelId { get; set; }
    public string TicketClosedFromDashboardMessage { get; set; } = string.Empty;
}

public sealed class GuildProfileApiResponse
{
    public Guid GuildId { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? CommunityType { get; set; }
    public string? SupportMessage { get; set; }
    public string? RulesUrl { get; set; }
    public string? WebsiteUrl { get; set; }
}

public sealed class AutoReplyRuleApiResponse
{
    public Guid Id { get; set; }
    public string Trigger { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public AutoReplyMatchMode MatchMode { get; set; }
    public AutoReplyScope Scope { get; set; }
    public bool Enabled { get; set; }
    public int Priority { get; set; }
}

public sealed class PendingTicketMessageApiResponse
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string DiscordGuildId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SenderDisplayName { get; set; }
    public string StaffReplyPrefix { get; set; } = string.Empty;
}

public sealed class TicketTimelineEventApiResponse
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public string? ActorDiscordUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? Content { get; set; }
    public Guid? RelatedTimelineEventId { get; set; }
    public string? MetadataJson { get; set; }
}

public sealed class RecordTicketMessageSentApiRequest
{
    public required string ChannelDiscordId { get; set; }
    public required string DiscordMessageId { get; set; }
    public required string AuthorDiscordUserId { get; set; }
    public string? AuthorDisplayName { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset? OccurredAt { get; set; }
}

public sealed class RecordTicketArchivePostedApiRequest
{
    public required string ArchiveChannelDiscordId { get; set; }
    public string? ActorDiscordUserId { get; set; }
    public string? ActorDisplayName { get; set; }
}

public sealed class AcknowledgeTicketMessageDeliveryApiRequest
{
    public bool Delivered { get; set; } = true;
    public string? FailureReason { get; set; }
}

public sealed class EvaluatePermissionsApiRequest
{
    public required string DiscordUserId { get; set; }
    public List<string> DiscordRoleIds { get; set; } = [];
}

public sealed class EvaluateDashboardAccessApiResponse
{
    public bool CanAccessTickets { get; set; }
    public bool CanAccessLogs { get; set; }
    public bool CanAccessModeration { get; set; }
}

public sealed class EvaluatePermissionsApiResponse
{
    public bool CanWarn { get; set; }
    public bool CanKick { get; set; }
    public bool CanTimeout { get; set; }
    public bool CanClearMessages { get; set; }
    public bool CanAccessModeration { get; set; }
    public bool CanViewWarnings { get; set; }
    public bool CanViewModerationCases { get; set; }
    public bool CanViewLogs { get; set; }
}
