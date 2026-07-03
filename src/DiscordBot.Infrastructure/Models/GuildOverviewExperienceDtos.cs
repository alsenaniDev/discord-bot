namespace DiscordBot.Infrastructure.Models;

public sealed class GuildOverviewExperienceDto
{
    public OverviewSubscriptionSummaryDto Subscription { get; init; } = new();

    public bool BotOnline { get; init; }

    public ActivationProgressDto Activation { get; init; } = new();

    public CommunityHealthDto Health { get; init; } = new();

    public IReadOnlyList<OverviewRecommendationDto> Recommendations { get; init; } = [];

    public IReadOnlyList<OverviewActivityItemDto> RecentActivity { get; init; } = [];
}

public sealed class OverviewSubscriptionSummaryDto
{
    public string PlanKey { get; init; } = "free";

    public string PlanName { get; init; } = "Free";

    public string Status { get; init; } = "Active";

    public DateTimeOffset? ExpiresAt { get; init; }

    public bool IsPaid { get; init; }

    public bool IsExpired { get; init; }
}

public sealed class ActivationStepDto
{
    public string Key { get; init; } = string.Empty;

    public string Phase { get; init; } = string.Empty;

    public bool Completed { get; init; }

    public int Weight { get; init; }

    public string ActionRoute { get; init; } = string.Empty;
}

public sealed class ActivationProgressDto
{
    public int ProgressPercent { get; init; }

    public bool IsActivated { get; init; }

    public string? CurrentStepKey { get; init; }

    public string PrimaryCtaKey { get; init; } = string.Empty;

    public string PrimaryActionRoute { get; init; } = string.Empty;

    public IReadOnlyList<ActivationStepDto> Steps { get; init; } = [];
}

public sealed class HealthFactorDto
{
    public string Key { get; init; } = string.Empty;

    public bool Passed { get; init; }

    public int PointsEarned { get; init; }

    public int PointsPossible { get; init; }

    public bool IsWarning { get; init; }
}

public sealed class CommunityHealthDto
{
    public int Score { get; init; }

    public string Level { get; init; } = "Critical";

    public IReadOnlyList<HealthFactorDto> Factors { get; init; } = [];
}

public sealed class OverviewRecommendationDto
{
    public string Id { get; init; } = string.Empty;

    public string Priority { get; init; } = "Medium";

    public string Route { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}

public sealed class OverviewActivityItemDto
{
    public string Type { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public DateTimeOffset OccurredAt { get; init; }
}
