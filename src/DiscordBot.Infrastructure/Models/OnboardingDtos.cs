using System.Text.Json;
using System.Text.Json.Serialization;

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

public sealed class DiscordGuildOnboardingDto
{
    public string DiscordGuildId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? IconUrl { get; set; }

    public bool IsOwner { get; set; }
    public bool CanManage { get; set; }

    public bool BotInstalled { get; set; }
    public Guid? PlatformGuildId { get; set; }

    public string Action { get; set; } = string.Empty; // "manage" | "add_bot"
    public string? InviteUrl { get; set; }
}

public sealed class DiscordUserGuildResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }

    [JsonPropertyName("owner")]
    public bool Owner { get; set; }

    [JsonPropertyName("permissions")]
    public JsonElement Permissions { get; set; }
}