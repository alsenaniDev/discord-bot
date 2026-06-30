namespace DiscordBot.Domain.Enums;

public enum LogEventType
{
    MemberJoined = 1,
    WelcomeSent = 2,
    AutoRoleAssigned = 3,
    TicketOpened = 4,
    TicketClosed = 5,
    WarningCreated = 10,
    MessagesCleared = 11,
    MemberKicked = 12,
    ModuleChanged = 20,
    SettingsUpdated = 21,
    ResourceSyncCompleted = 30,
    ReactionRoleCreated = 40,
    ReactionRoleAssigned = 41,
    ReactionRoleRemoved = 42,
    ReactionRoleDeleted = 43
}
