namespace DiscordBot.Domain.Enums;

[Flags]
public enum GuildPermissions
{
    None = 0,
    Warn = 1 << 0,
    Kick = 1 << 1,
    Timeout = 1 << 2,
    ClearMessages = 1 << 3,
    AccessModeration = 1 << 4,
    AccessLogs = 1 << 5,
    AccessTickets = 1 << 6,
    ManagePermissionRoles = 1 << 7
}
