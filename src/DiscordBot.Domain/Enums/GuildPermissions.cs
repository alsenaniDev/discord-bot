namespace DiscordBot.Domain.Enums;

[Flags]
public enum GuildPermissions
{
    None = 0,

    // Bot moderation commands (bits 0-3 preserved for existing stored values)
    UseWarn = 1 << 0,
    UseKick = 1 << 1,
    UseTimeout = 1 << 2,
    UseClearMessages = 1 << 3,

    // Dashboard module access (bits 4-7 preserved for existing stored values)
    ManageModeration = 1 << 4,
    ViewLogs = 1 << 5,
    ViewTickets = 1 << 6,
    ManagePermissionRoles = 1 << 7,

    // General dashboard
    AccessDashboard = 1 << 8,
    ViewServer = 1 << 9,
    ManageSettings = 1 << 10,
    ManageModules = 1 << 11,

    // Tickets
    ReplyToTickets = 1 << 12,
    CloseTickets = 1 << 13,
    ManageTickets = 1 << 14,

    // Moderation
    UseBan = 1 << 15,

    // Logs
    ClearLogs = 1 << 16,

    // Reaction roles
    ManageReactionRoles = 1 << 17,

    // Bot moderation views (merged from ModerationPermissionRoles)
    ViewWarnings = 1 << 18,
    ViewModerationCases = 1 << 19
}
