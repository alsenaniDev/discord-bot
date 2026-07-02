namespace DiscordBot.Domain.Entities;

/// <summary>
/// Subscription plan catalog entry.
/// </summary>
public class SubscriptionPlan : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>JSON array of module keys, or ["*"] for all modules.</summary>
    public string AllowedModulesJson { get; set; } = "[]";

    public decimal MonthlyPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<GuildSubscription> GuildSubscriptions { get; set; } = [];
}
