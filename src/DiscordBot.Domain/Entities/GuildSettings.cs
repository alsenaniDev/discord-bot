using DiscordBot.Domain.Constants;

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
    public string? TicketArchiveChannelId { get; set; }
    public string TicketWelcomeTitle { get; set; } = TicketMessageDefaults.WelcomeTitle;
    public string TicketWelcomeMessage { get; set; } = TicketMessageDefaults.WelcomeMessage;
    public string TicketClosedMessage { get; set; } = TicketMessageDefaults.ClosedMessage;
    public string TicketClosedFromDashboardMessage { get; set; } = TicketMessageDefaults.ClosedFromDashboardMessage;
    public string TicketStaffReplyPrefix { get; set; } = TicketMessageDefaults.StaffReplyPrefix;

    // Legacy member command panel. TODO: Remove these columns in a cleanup migration after GuildPanels is stable.
    public bool CommandPanelEnabled { get; set; }
    public string? CommandPanelChannelId { get; set; }
    public string? CommandPanelMessageId { get; set; }
    public string CommandPanelTitle { get; set; } = "How can we help?";
    public string CommandPanelDescription { get; set; } = "Use the buttons below — no commands needed.";
    public string? CommandPanelImageUrl { get; set; }
    public string CommandPanelButtonsJson { get; set; } =
        """[{"id":"ticket-open","action":"ticket_open","label":"Create Ticket","style":"Success","enabled":true,"order":0},{"id":"ticket-help","action":"ticket_help","label":"Ticket Help","style":"Secondary","enabled":true,"order":1}]""";
    public bool CommandPanelRefreshRequested { get; set; }
}
