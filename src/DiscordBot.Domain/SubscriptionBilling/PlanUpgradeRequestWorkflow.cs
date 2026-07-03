using DiscordBot.Domain.Enums;

namespace DiscordBot.Domain.SubscriptionBilling;

public static class PlanUpgradeRequestWorkflow
{
    public static readonly PlanUpgradeRequestStatus[] TerminalStatuses =
    [
        PlanUpgradeRequestStatus.Activated,
        PlanUpgradeRequestStatus.Rejected,
        PlanUpgradeRequestStatus.Cancelled,
        PlanUpgradeRequestStatus.Expired
    ];

    public static readonly PlanUpgradeRequestStatus[] ActiveStatuses =
    [
        PlanUpgradeRequestStatus.Requested,
        PlanUpgradeRequestStatus.PendingPayment,
        PlanUpgradeRequestStatus.PaymentSubmitted,
        PlanUpgradeRequestStatus.UnderReview,
        PlanUpgradeRequestStatus.Approved
    ];

    public static bool IsTerminal(PlanUpgradeRequestStatus status) =>
        TerminalStatuses.Contains(status);

    public static bool IsActive(PlanUpgradeRequestStatus status) =>
        ActiveStatuses.Contains(status);

    public static bool CanApprove(PlanUpgradeRequestStatus status, bool hasAdminOverride) =>
        status switch
        {
            PlanUpgradeRequestStatus.UnderReview => true,
            PlanUpgradeRequestStatus.PaymentSubmitted => true,
            PlanUpgradeRequestStatus.PendingPayment => hasAdminOverride,
            _ => false
        };

    public static bool CanSubmitPayment(PlanUpgradeRequestStatus status) =>
        status is PlanUpgradeRequestStatus.PendingPayment;

    public static void EnsureCanSubmitPayment(PlanUpgradeRequestStatus status)
    {
        if (!CanSubmitPayment(status))
        {
            throw new InvalidOperationException(
                $"Payment reference cannot be submitted when the subscription change is '{status}'.");
        }
    }

    public static bool CanReject(PlanUpgradeRequestStatus status) =>
        status is PlanUpgradeRequestStatus.UnderReview
            or PlanUpgradeRequestStatus.PaymentSubmitted
            or PlanUpgradeRequestStatus.PendingPayment;

    public static bool CanCancel(PlanUpgradeRequestStatus status) =>
        status is PlanUpgradeRequestStatus.Requested
            or PlanUpgradeRequestStatus.PendingPayment
            or PlanUpgradeRequestStatus.PaymentSubmitted
            or PlanUpgradeRequestStatus.UnderReview;

    public static bool CanExpire(PlanUpgradeRequestStatus status) =>
        status is PlanUpgradeRequestStatus.Requested
            or PlanUpgradeRequestStatus.PendingPayment;

    public static void EnsureCanApprove(PlanUpgradeRequestStatus status, bool hasAdminOverride)
    {
        if (!CanApprove(status, hasAdminOverride))
        {
            if (status == PlanUpgradeRequestStatus.PendingPayment)
            {
                throw new InvalidOperationException(
                    "Admin override is required to approve a subscription change before payment proof is submitted.");
            }

            throw new InvalidOperationException(
                $"Subscription change cannot be approved from status '{status}'.");
        }
    }

    public static void EnsureCanReject(PlanUpgradeRequestStatus status)
    {
        if (!CanReject(status))
        {
            throw new InvalidOperationException(
                $"Upgrade request cannot be rejected from status '{status}'.");
        }
    }

    public static void EnsureCanCancel(PlanUpgradeRequestStatus status)
    {
        if (!CanCancel(status))
        {
            throw new InvalidOperationException(
                $"Upgrade request cannot be cancelled from status '{status}'.");
        }
    }

    public static void EnsureTransition(
        PlanUpgradeRequestStatus from,
        PlanUpgradeRequestStatus to)
    {
        var allowed = (from, to) switch
        {
            (PlanUpgradeRequestStatus.Requested, PlanUpgradeRequestStatus.PendingPayment) => true,
            (PlanUpgradeRequestStatus.PendingPayment, PlanUpgradeRequestStatus.PaymentSubmitted) => true,
            (PlanUpgradeRequestStatus.PaymentSubmitted, PlanUpgradeRequestStatus.UnderReview) => true,
            (PlanUpgradeRequestStatus.UnderReview, PlanUpgradeRequestStatus.Approved) => true,
            (PlanUpgradeRequestStatus.PendingPayment, PlanUpgradeRequestStatus.Approved) => true,
            (PlanUpgradeRequestStatus.PaymentSubmitted, PlanUpgradeRequestStatus.Approved) => true,
            (PlanUpgradeRequestStatus.Approved, PlanUpgradeRequestStatus.Activated) => true,
            (PlanUpgradeRequestStatus.UnderReview, PlanUpgradeRequestStatus.PendingPayment) => true,
            (PlanUpgradeRequestStatus.UnderReview, PlanUpgradeRequestStatus.Rejected) => true,
            (PlanUpgradeRequestStatus.PaymentSubmitted, PlanUpgradeRequestStatus.Rejected) => true,
            (PlanUpgradeRequestStatus.PendingPayment, PlanUpgradeRequestStatus.Rejected) => true,
            (PlanUpgradeRequestStatus.Requested, PlanUpgradeRequestStatus.Cancelled) => true,
            (PlanUpgradeRequestStatus.PendingPayment, PlanUpgradeRequestStatus.Cancelled) => true,
            (PlanUpgradeRequestStatus.PaymentSubmitted, PlanUpgradeRequestStatus.Cancelled) => true,
            (PlanUpgradeRequestStatus.UnderReview, PlanUpgradeRequestStatus.Cancelled) => true,
            (PlanUpgradeRequestStatus.Requested, PlanUpgradeRequestStatus.Expired) => true,
            (PlanUpgradeRequestStatus.PendingPayment, PlanUpgradeRequestStatus.Expired) => true,
            _ => false
        };

        if (!allowed)
        {
            throw new InvalidOperationException(
                $"Invalid upgrade request transition from '{from}' to '{to}'.");
        }
    }
}
