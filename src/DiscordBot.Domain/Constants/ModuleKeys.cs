namespace DiscordBot.Domain.Constants;

public static class ModuleKeys
{
    public const string Welcome = "welcome";
    public const string Tickets = "tickets";
    public const string Moderation = "moderation";
    public const string Logs = "logs";
    public const string AutoRole = "auto-role";
    public const string ReactionRoles = "reaction-roles";

    public static readonly IReadOnlyList<string> All =
    [
        Welcome,
        Tickets,
        Moderation,
        Logs,
        AutoRole,
        ReactionRoles
    ];
}
