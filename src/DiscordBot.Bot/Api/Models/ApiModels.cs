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
    public string? MetadataJson { get; set; }
}

public sealed class TicketResponse
{
    public Guid Id { get; set; }
    public Guid GuildId { get; set; }
    public int TicketNumber { get; set; }
    public string OwnerDiscordUserId { get; set; } = string.Empty;
    public string ChannelDiscordId { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}

public sealed class CreateTicketApiRequest
{
    public required string DiscordGuildId { get; set; }
    public required string OwnerDiscordUserId { get; set; }
    public required string ChannelDiscordId { get; set; }
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

public sealed class CreateWarningApiRequest
{
    public required string DiscordGuildId { get; set; }
    public required string TargetDiscordUserId { get; set; }
    public required string ModeratorDiscordUserId { get; set; }
    public required string Reason { get; set; }
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
