namespace DiscordBot.Infrastructure.Models;

public sealed class GuildSummaryDto
{
    public Guid Id { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public bool IsActive { get; init; }
}

public sealed class GuildSettingsDto
{
    public Guid GuildId { get; init; }
    public bool WelcomeEnabled { get; init; }
    public string? WelcomeChannelId { get; init; }
    public string WelcomeMessage { get; init; } = string.Empty;
    public bool AutoRoleEnabled { get; init; }
    public string? AutoRoleId { get; init; }
    public bool LogsEnabled { get; init; }
    public string? LogChannelId { get; init; }
    public bool TicketsEnabled { get; init; }
    public string? TicketCategoryId { get; init; }
}

public sealed class UpdateGuildSettingsRequest
{
    public bool WelcomeEnabled { get; set; }
    public string? WelcomeChannelId { get; set; }
    public string WelcomeMessage { get; set; } = "Welcome {user} to {server}!";
    public bool AutoRoleEnabled { get; set; }
    public string? AutoRoleId { get; set; }
    public bool LogsEnabled { get; set; }
    public string? LogChannelId { get; set; }
    public string? TicketCategoryId { get; set; }
}

public sealed class GuildOverviewDto
{
    public string Name { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset? ResourcesSyncedAt { get; init; }
    public int TotalChannels { get; init; }
    public int TotalRoles { get; init; }
    public int TotalTickets { get; init; }
    public int OpenTickets { get; init; }
    public int ClosedTickets { get; init; }
    public bool WelcomeEnabled { get; init; }
    public bool AutoRoleEnabled { get; init; }
    public bool LogsEnabled { get; init; }
    public bool TicketsEnabled { get; init; }
    public OnboardingChecklistDto? Onboarding { get; init; }
}
