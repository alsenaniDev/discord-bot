namespace DiscordBot.Domain.Entities;

/// <summary>
/// A Discord server (guild) where the bot is installed.
/// Each guild has its own settings — this is our multi-tenant boundary in v1.
/// </summary>
public class Guild : BaseEntity
{
    /// <summary>Discord snowflake for the server.</summary>
    public required string DiscordGuildId { get; set; }

    public required string Name { get; set; }
    public string? IconUrl { get; set; }

    /// <summary>Bot-managed display name shown in embeds (not Discord server rename).</summary>
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? CommunityType { get; set; }
    public string? SupportMessage { get; set; }
    public string? RulesUrl { get; set; }
    public string? WebsiteUrl { get; set; }

    /// <summary>Discord user id of the server owner.</summary>
    public required string OwnerDiscordUserId { get; set; }

    /// <summary>False when the bot leaves the server.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Set when the dashboard requests a resource sync from the bot.</summary>
    public bool ResourceSyncRequested { get; set; }

    public DateTimeOffset? ResourcesSyncedAt { get; set; }

    public GuildSettings? Settings { get; set; }
    public ICollection<LogEntry> Logs { get; set; } = [];
    public ICollection<Ticket> Tickets { get; set; } = [];
    public ICollection<DiscordChannel> Channels { get; set; } = [];
    public ICollection<DiscordRole> Roles { get; set; } = [];
    public ICollection<Warning> Warnings { get; set; } = [];
    public ICollection<ModerationCase> ModerationCases { get; set; } = [];
    public ICollection<GuildModule> GuildModules { get; set; } = [];
    public ICollection<ReactionRole> ReactionRoles { get; set; } = [];
    public ICollection<GuildPanel> Panels { get; set; } = [];
    public GuildSubscription? Subscription { get; set; }
}
