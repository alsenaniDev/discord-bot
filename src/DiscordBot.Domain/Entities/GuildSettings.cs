namespace DiscordBot.Domain.Entities;

/// <summary>
/// Per-guild bot configuration edited from the dashboard.
/// One row per guild — welcome, auto-role, and log channel settings.
/// </summary>
public class GuildSettings : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    // Welcome messages
    public bool WelcomeEnabled { get; set; }
    public string? WelcomeChannelId { get; set; }
    public string WelcomeMessage { get; set; } = "Welcome {user} to {server}!";

    // Auto role on member join
    public bool AutoRoleEnabled { get; set; }
    public string? AutoRoleId { get; set; }

    // Moderation / event logs
    public bool LogsEnabled { get; set; }
    public string? LogChannelId { get; set; }

    // Support tickets
    public bool TicketsEnabled { get; set; }
    public string? TicketCategoryId { get; set; }
}
