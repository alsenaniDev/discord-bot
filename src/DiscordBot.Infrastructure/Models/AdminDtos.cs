namespace DiscordBot.Infrastructure.Models;

public sealed class AdminStatsDto
{
    public int TotalGuilds { get; init; }
    public int ActiveGuilds { get; init; }
    public int TotalUsers { get; init; }
    public int TotalTickets { get; init; }
    public int OpenTickets { get; init; }
    public IReadOnlyList<AdminPlanCountDto> PlanCounts { get; init; } = [];
    public IReadOnlyList<AdminModuleUsageDto> ModuleUsageCounts { get; init; } = [];
}

public sealed class AdminPlanCountDto
{
    public string PlanKey { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class AdminModuleUsageDto
{
    public string ModuleKey { get; init; } = string.Empty;
    public string ModuleName { get; init; } = string.Empty;
    public int EnabledGuildCount { get; init; }
}

public class AdminGuildSummaryDto
{
    public Guid Id { get; init; }
    public string DiscordGuildId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string OwnerDiscordUserId { get; init; } = string.Empty;
    public string PlanKey { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public int EnabledModulesCount { get; init; }
    public int TicketsCount { get; init; }
    public DateTimeOffset? ResourcesSyncedAt { get; init; }
    public bool IsActive { get; init; }
}

public sealed class AdminGuildDetailDto : AdminGuildSummaryDto
{
    public IReadOnlyList<string> AllowedModules { get; init; } = [];
    public int OpenTicketsCount { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AdminUserDto
{
    public Guid Id { get; init; }
    public string DiscordUserId { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string? GlobalName { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
