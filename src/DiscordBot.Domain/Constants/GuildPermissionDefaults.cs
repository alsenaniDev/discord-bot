using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Constants;

public static class GuildPermissionDefaults
{
    public const GuildPermissions OwnerPermissions =
        GuildPermissions.UseWarn
        | GuildPermissions.UseKick
        | GuildPermissions.UseTimeout
        | GuildPermissions.UseBan
        | GuildPermissions.UseClearMessages
        | GuildPermissions.ManageModeration
        | GuildPermissions.ViewLogs
        | GuildPermissions.ClearLogs
        | GuildPermissions.ViewTickets
        | GuildPermissions.ReplyToTickets
        | GuildPermissions.CloseTickets
        | GuildPermissions.ManageTickets
        | GuildPermissions.ManagePermissionRoles
        | GuildPermissions.AccessDashboard
        | GuildPermissions.ViewServer
        | GuildPermissions.ManageSettings
        | GuildPermissions.ManageModules
        | GuildPermissions.ManageReactionRoles
        | GuildPermissions.ViewWarnings
        | GuildPermissions.ViewModerationCases;
}
