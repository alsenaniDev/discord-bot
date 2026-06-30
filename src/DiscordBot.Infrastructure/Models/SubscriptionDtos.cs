namespace DiscordBot.Infrastructure.Models;

public sealed class SubscriptionPlanDto
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> AllowedModules { get; init; } = [];
    public bool IsActive { get; init; }
}

public sealed class GuildSubscriptionDto
{
    public Guid GuildId { get; init; }
    public string PlanKey { get; init; } = string.Empty;
    public string PlanName { get; init; } = string.Empty;
    public string PlanDescription { get; init; } = string.Empty;
    public IReadOnlyList<string> AllowedModules { get; init; } = [];
}

public sealed class UpdateGuildSubscriptionRequest
{
    public required string PlanKey { get; set; }
}
