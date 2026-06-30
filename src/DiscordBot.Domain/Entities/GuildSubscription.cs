namespace DiscordBot.Domain.Entities;

/// <summary>
/// Links a guild to its current subscription plan.
/// </summary>
public class GuildSubscription : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public Guid SubscriptionPlanId { get; set; }
    public SubscriptionPlan SubscriptionPlan { get; set; } = null!;
}
