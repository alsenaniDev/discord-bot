namespace DiscordBot.Domain.Constants;

public static class CommandPanelActions
{
    public const string TicketOpen = "ticket_open";
    public const string TicketHelp = "ticket_help";
    public const string Ping = "ping";
    public const string ServerInfo = "server_info";
    public const string ModerationHelp = "moderation_help";
    public const string ReactionRolesHelp = "reaction_roles_help";
    public const string PlatformHelp = "platform_help";

    public static readonly IReadOnlyList<string> All =
    [
        TicketOpen,
        TicketHelp,
        Ping,
        ServerInfo,
        ModerationHelp,
        ReactionRolesHelp,
        PlatformHelp
    ];
}
