namespace DiscordBot.Domain.Enums;

public enum PlanUpgradeRequestStatus
{
    Requested = 0,
    PendingPayment = 1,
    PaymentSubmitted = 2,
    UnderReview = 3,
    Approved = 4,
    Activated = 5,
    Rejected = 6,
    Cancelled = 7,
    Expired = 8
}
