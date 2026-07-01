using DiscordBot.Domain.Enums;

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

    public GuildSubscriptionStatus Status { get; set; } = GuildSubscriptionStatus.Active;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public Guid? ApprovedRequestId { get; set; }
    public PlanUpgradeRequest? ApprovedRequest { get; set; }
}
