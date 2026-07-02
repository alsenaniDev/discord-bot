using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class CreatePlanUpgradeRequest
{
    public required string PlanKey { get; set; }
    public int DurationMonths { get; set; }
}

public sealed class PlanUpgradeRequestDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string RequestedPlanKey { get; init; } = string.Empty;
    public string RequestedPlanName { get; init; } = string.Empty;
    public string CurrentPlanKey { get; init; } = string.Empty;
    public string CurrentPlanName { get; init; } = string.Empty;
    public string RequestedByUsername { get; init; } = string.Empty;
    public int DurationMonths { get; init; }
    public decimal RequestedPlanMonthlyPrice { get; init; }
    public decimal EstimatedTotalPrice { get; init; }
    public PlanUpgradeRequestStatus Status { get; init; }
    public string? AdminNote { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public DateTimeOffset EstimatedExpiresAtIfApprovedToday { get; init; }
}

public sealed class AdminPlanUpgradeRequestDto
{
    public Guid Id { get; init; }
    public Guid GuildId { get; init; }
    public string GuildName { get; init; } = string.Empty;
    public string CurrentPlanKey { get; init; } = string.Empty;
    public string CurrentPlanName { get; init; } = string.Empty;
    public string RequestedPlanKey { get; init; } = string.Empty;
    public string RequestedPlanName { get; init; } = string.Empty;
    public string RequestedByUsername { get; init; } = string.Empty;
    public string RequestedByDiscordUserId { get; init; } = string.Empty;
    public int DurationMonths { get; init; }
    public decimal RequestedPlanMonthlyPrice { get; init; }
    public decimal EstimatedTotalPrice { get; init; }
    public PlanUpgradeRequestStatus Status { get; init; }
    public string? AdminNote { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ReviewedAt { get; init; }
    public DateTimeOffset EstimatedExpiresAtIfApprovedToday { get; init; }
}

public sealed class ReviewPlanUpgradeRequest
{
    public string? AdminNote { get; set; }
}
