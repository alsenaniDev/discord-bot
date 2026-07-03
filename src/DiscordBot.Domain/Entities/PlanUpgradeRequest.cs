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

    public SubscriptionChangeType ChangeType { get; set; } = SubscriptionChangeType.Upgrade;

    public int DurationMonths { get; set; }

    public decimal RequestedPlanMonthlyPrice { get; set; }

    public decimal EstimatedTotalAmount { get; set; }

    public PlanUpgradeRequestStatus Status { get; set; } = PlanUpgradeRequestStatus.Requested;

    public DateTimeOffset? RequestExpiresAt { get; set; }

    public string? PaymentReference { get; set; }

    public DateTimeOffset? PaymentSubmittedAt { get; set; }

    public string? AdminNote { get; set; }

    public string? AdminOverrideReason { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public Guid? ReviewedByAdminId { get; set; }
    public User? ReviewedByAdmin { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public Guid? CancelledByUserId { get; set; }
    public User? CancelledByUser { get; set; }
}
