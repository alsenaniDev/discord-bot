namespace DiscordBot.Infrastructure.Models;

public sealed class OnboardingChecklistDto
{
    public bool BotInvited { get; init; }
    public bool ResourcesSynced { get; init; }
    public bool PlanSelected { get; init; }
    public bool ModulesEnabled { get; init; }
    public bool WelcomeConfigured { get; init; }
    public bool TicketsConfigured { get; init; }
    public int CompletedCount { get; init; }
    public int TotalCount { get; init; } = 6;
    public int ProgressPercent { get; init; }
}

public sealed class GuildOnboardingDto
{
    public Guid GuildId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? IconUrl { get; init; }
    public OnboardingChecklistDto Checklist { get; init; } = new();
}

public sealed class OnboardingStatusDto
{
    public bool HasGuilds { get; init; }
    public string BotInviteUrl { get; init; } = string.Empty;
    public string DashboardUrl { get; init; } = string.Empty;
    public IReadOnlyList<GuildOnboardingDto> Guilds { get; init; } = [];
}
