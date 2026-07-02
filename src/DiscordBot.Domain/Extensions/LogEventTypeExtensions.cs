using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Extensions;

public static class LogEventTypeExtensions
{
    public static bool IsCritical(LogEventType type) =>
        type is LogEventType.WarningCreated
            or LogEventType.MessagesCleared
            or LogEventType.MemberKicked
            or LogEventType.ModuleChanged
            or LogEventType.SettingsUpdated;

    public static string GetLabel(LogEventType type) =>
        type switch
        {
            LogEventType.MemberJoined => "Member joined",
            LogEventType.WelcomeSent => "Welcome sent",
            LogEventType.AutoRoleAssigned => "Auto role assigned",
            LogEventType.TicketOpened => "Ticket opened",
            LogEventType.TicketClosed => "Ticket closed",
            LogEventType.TicketArchived => "Ticket archived",
            LogEventType.WarningCreated => "Warning created",
            LogEventType.MessagesCleared => "Messages cleared",
            LogEventType.MemberKicked => "Member kicked",
            LogEventType.ModuleChanged => "Module changed",
            LogEventType.SettingsUpdated => "Settings updated",
            LogEventType.ResourceSyncCompleted => "Resource sync completed",
            LogEventType.ReactionRoleCreated => "Reaction role created",
            LogEventType.ReactionRoleAssigned => "Reaction role assigned",
            LogEventType.ReactionRoleRemoved => "Reaction role removed",
            LogEventType.ReactionRoleDeleted => "Reaction role deleted",
            _ => type.ToString()
        };
}
