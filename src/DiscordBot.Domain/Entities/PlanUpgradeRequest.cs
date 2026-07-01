using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.Entities;

public class PlanUpgradeRequest : BaseEntity
{
    public Guid GuildId { get; set; }
    public Guild Guild { get; set; } = null!;

    public Guid RequestedPlanId { get; set; }
    public SubscriptionPlan RequestedPlan { get; set; } = null!;

    public Guid CurrentPlanId { get; set; }
    public SubscriptionPlan CurrentPlan { get; set; } = null!;

    public Guid RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;

    public int DurationMonths { get; set; }

    public PlanUpgradeRequestStatus Status { get; set; } = PlanUpgradeRequestStatus.Pending;

    public string? AdminNote { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public Guid? ReviewedByAdminId { get; set; }
    public User? ReviewedByAdmin { get; set; }
}
