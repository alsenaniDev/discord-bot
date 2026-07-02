using DiscordBot.Domain.Enums;

namespace DiscordBot.Infrastructure.Models;

public sealed class SubscriptionPlanDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> AllowedModules { get; init; } = [];
    public bool IsActive { get; init; }
    public decimal MonthlyPrice { get; init; }
}

public sealed class AdminSubscriptionPlanDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> AllowedModules { get; init; } = [];
    public bool IsActive { get; init; }
    public decimal MonthlyPrice { get; init; }
    public int SubscriberCount { get; init; }
}

public sealed class CreateSubscriptionPlanRequest
{
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public IReadOnlyList<string> AllowedModules { get; set; } = [];
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpdateSubscriptionPlanRequest
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public IReadOnlyList<string> AllowedModules { get; set; } = [];
    public decimal MonthlyPrice { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class GuildSubscriptionDto
{
    public Guid GuildId { get; init; }
    public string PlanKey { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public string PlanDescription { get; init; } = string.Empty;
    public IReadOnlyList<string> AllowedModules { get; init; } = [];
    public GuildSubscriptionStatus Status { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
}

public sealed class UpdateGuildSubscriptionRequest
{
    public required string PlanKey { get; set; }
}

public sealed class ExtendGuildSubscriptionRequest
{
    public int Months { get; set; }
}
