namespace DiscordBot.Bot.UI;

/// <summary>
/// Custom IDs for buttons, select menus, and modals.
/// Format: domain:action[:context]
/// </summary>
public static class DiscordCustomIds
{
    public const string TicketCreate = "ticket:create";
    public const string TicketClosePrefix = "ticket:close:";
    public const string TicketSelectPrefix = "ticket:select:";
    public const string TicketCloseModalPrefix = "ticket:close_modal:";

    public const string ReactionRoleTogglePrefix = "reaction-role:toggle:";
    public const string PanelPrefix = "panel:";

    public const string TicketSelectClose = "close";
    public const string TicketSelectHelp = "help";

    public static string TicketCloseButton(ulong channelId) => $"{TicketClosePrefix}{channelId}";

    public static string TicketSelectMenu(ulong channelId) => $"{TicketSelectPrefix}{channelId}";

    public static string TicketCloseModal(ulong channelId) => $"{TicketCloseModalPrefix}{channelId}";

    public static string ReactionRoleToggle(Guid panelId) => $"{ReactionRoleTogglePrefix}{panelId:D}";

    public static string PanelButton(string action, string buttonId) =>
        $"{PanelPrefix}{action}:{buttonId}";

    public static bool TryParsePanelAction(string customId, out string action)
    {
        action = string.Empty;

        if (!customId.StartsWith(PanelPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var remainder = customId[PanelPrefix.Length..];
        var separatorIndex = remainder.IndexOf(':');
        action = separatorIndex >= 0 ? remainder[..separatorIndex] : remainder;
        return !string.IsNullOrWhiteSpace(action);
    }

    public static bool TryParseReactionRoleToggleId(string customId, out Guid panelId)
    {
        panelId = Guid.Empty;

        if (!customId.StartsWith(ReactionRoleTogglePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return Guid.TryParse(customId[ReactionRoleTogglePrefix.Length..], out panelId);
    }

    public static bool TryParseChannelId(string customId, string prefix, out ulong channelId)
    {
        channelId = 0;

        if (!customId.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return ulong.TryParse(customId[prefix.Length..], out channelId);
    }
}
