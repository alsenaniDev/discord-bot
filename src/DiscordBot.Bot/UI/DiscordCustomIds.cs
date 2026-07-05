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
    public const string WorkflowPrefix = "workflow:";

    public const string TicketSelectClose = "close";
    public const string TicketSelectHelp = "help";

    public static string TicketCloseButton(ulong channelId) => $"{TicketClosePrefix}{channelId}";

    public static string TicketSelectMenu(ulong channelId) => $"{TicketSelectPrefix}{channelId}";

    public static string TicketCloseModal(ulong channelId) => $"{TicketCloseModalPrefix}{channelId}";

    public static string ReactionRoleToggle(Guid panelId) => $"{ReactionRoleTogglePrefix}{panelId:D}";

    public static string PanelButton(string action, string buttonId) =>
        $"{PanelPrefix}{action}:{buttonId}";

    public static string PanelButton(Guid panelId, Guid buttonId) =>
        $"panel:{panelId:D}:button:{buttonId:D}";

    public static bool TryParsePanelButton(string customId, out Guid panelId, out Guid buttonId)
    {
        panelId = Guid.Empty; buttonId = Guid.Empty;
        var parts = customId.Split(':');
        return parts.Length == 4 && parts[0] == "panel" && parts[2] == "button"
            && Guid.TryParse(parts[1], out panelId) && Guid.TryParse(parts[3], out buttonId);
    }

    public static string WorkflowConfirm(Guid workflowId, ulong guildId) => $"workflow:confirm:{workflowId:D}:{guildId}";
    public static string WorkflowCancel(Guid workflowId, ulong guildId) => $"workflow:cancel:{workflowId:D}:{guildId}";
    public static bool TryParseWorkflowControl(string customId, out bool confirm, out Guid workflowId, out ulong guildId)
    {
        confirm = false; workflowId = Guid.Empty; guildId = 0; var p = customId.Split(':');
        if (p.Length != 4 || p[0] != "workflow" || (p[1] != "confirm" && p[1] != "cancel")) return false;
        confirm = p[1] == "confirm"; return Guid.TryParse(p[2], out workflowId) && ulong.TryParse(p[3], out guildId);
    }

    public static string WorkflowConversationCancel(Guid workflowId, Guid conversationId) =>
        $"workflow:{workflowId:N}:conversation:{conversationId:N}:cancel";

    public static bool TryParseWorkflowConversationCancel(string customId, out Guid workflowId, out Guid conversationId)
    {
        workflowId = Guid.Empty; conversationId = Guid.Empty; var p = customId.Split(':');
        return p.Length == 5 && p[0] == "workflow" && p[2] == "conversation" && p[4] == "cancel"
            && Guid.TryParse(p[1], out workflowId) && Guid.TryParse(p[3], out conversationId);
    }

    public static string WorkflowQuestionAnswer(Guid workflowId, string conversationToken, string questionToken, string answer) =>
        $"workflow:{workflowId:N}:conversation:{conversationToken}:question:{questionToken}:answer:{answer}";

    public static bool TryParseWorkflowQuestionAnswer(string customId, out Guid workflowId, out string conversationToken, out string questionToken, out string answer)
    {
        workflowId = Guid.Empty; conversationToken = questionToken = answer = string.Empty; var p = customId.Split(':');
        if (p.Length != 8 || p[0] != "workflow" || p[2] != "conversation" || p[4] != "question" || p[6] != "answer" || !Guid.TryParse(p[1], out workflowId)) return false;
        conversationToken = p[3]; questionToken = p[5]; answer = p[7];
        return conversationToken.Length > 0 && questionToken.Length > 0 && answer.Length > 0;
    }

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
