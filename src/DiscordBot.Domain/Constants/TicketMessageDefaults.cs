namespace DiscordBot.Domain.Constants;

public static class TicketMessageDefaults
{
    public const string WelcomeTitle = "Ticket #{ticket}";
    public const string WelcomeMessage =
        "{mention}, thanks for reaching out.\n\n" +
        "A staff member will assist you shortly. Use the **Close ticket** button when your issue is resolved.";

    public const string ClosedMessage =
        "Ticket #{ticket} was closed by {mention}.";

    public const string ClosedFromDashboardMessage =
        "Ticket #{ticket} was closed from the dashboard. This channel will be deleted shortly.";

    public const string StaffReplyPrefix = "**{staff}** replied from the dashboard:";
}
