using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Constants;

public static class GuildPermissionDefaults
{
    public const GuildPermissions OwnerPermissions =
        GuildPermissions.Warn
        | GuildPermissions.Kick
        | GuildPermissions.Timeout
        | GuildPermissions.ClearMessages
        | GuildPermissions.AccessModeration
        | GuildPermissions.AccessLogs
        | GuildPermissions.AccessTickets
        | GuildPermissions.ManagePermissionRoles;
}
