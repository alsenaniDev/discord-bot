using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Domain.SubscriptionBilling;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DiscordBot.Infrastructure.Services;

public interface IPlanUpgradeRequestService
{
    Task<PlanUpgradeRequestDto?> CreateRequestAsync(
        Guid guildId,
        string ownerDiscordUserId,
        Guid requestedByUserId,
        string planKey,
        int durationMonths,
        SubscriptionChangeType? changeType = null,
        CancellationToken cancellationToken = default);

    Task<PlanUpgradeRequestDto?> GetCurrentChangeRequestAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<GuildSubscriptionStatusDto?> GetSubscriptionStatusAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<PlanUpgradeRequestDto?> SubmitPaymentReferenceAsync(
        Guid guildId,
        Guid requestId,
        string ownerDiscordUserId,
        string paymentReference,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanUpgradeRequestDto>> GetGuildRequestsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPlanUpgradeRequestDto>> GetAllRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<PlanUpgradeRequestDto?> CancelRequestAsync(
        Guid guildId,
        Guid requestId,
        string ownerDiscordUserId,
        Guid cancelledByUserId,
        CancellationToken cancellationToken = default);

    Task<AdminPlanUpgradeRequestDto?> CancelRequestAsAdminAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
        CancellationToken cancellationToken = default);

    Task<AdminPlanUpgradeRequestDto?> ApproveAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
        string? adminOverrideReason,
        CancellationToken cancellationToken = default);

    Task<AdminPlanUpgradeRequestDto?> RejectAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
        CancellationToken cancellationToken = default);
}

public class PlanUpgradeRequestService : IPlanUpgradeRequestService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<PlanUpgradeRequestService> _logger;

    public PlanUpgradeRequestService(
        AppDbContext dbContext,
        IGuildAccessService guildAccessService,
        ISubscriptionService subscriptionService,
        ILogger<PlanUpgradeRequestService> logger)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<PlanUpgradeRequestDto?> CreateRequestAsync(
        Guid guildId,
        string ownerDiscordUserId,
        Guid requestedByUserId,
        string planKey,
        int durationMonths,
        SubscriptionChangeType? changeType = null,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, ownerDiscordUserId, cancellationToken))
        {
            return null;
        }

        if (!SubscriptionDurations.IsValid(durationMonths))
        {
            return null;
        }

        var requestedPlan = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Key == planKey && p.IsActive, cancellationToken);

        if (requestedPlan is null || requestedPlan.Key == PlanKeys.Free)
        {
            return null;
        }

        await _subscriptionService.EnsureGuildSubscriptionAsync(guildId, cancellationToken);

        var currentSubscription = await _dbContext.GuildSubscriptions
            .AsNoTracking()
            .Include(gs => gs.SubscriptionPlan)
            .FirstAsync(gs => gs.GuildId == guildId, cancellationToken);

        var isSamePlan = currentSubscription.SubscriptionPlanId == requestedPlan.Id;
        var resolvedChangeType = changeType ?? SubscriptionChangeType.Upgrade;

        if (isSamePlan)
        {
            if (requestedPlan.Key == PlanKeys.Free)
            {
                return null;
            }

            resolvedChangeType = SubscriptionChangeType.Renewal;
        }
        else if (resolvedChangeType == SubscriptionChangeType.Renewal)
        {
            throw new InvalidOperationException(
                "Renewal requires the same plan as the current subscription.");
        }
        else if (resolvedChangeType == SubscriptionChangeType.Upgrade)
        {
            // Upgrade to a different plan — allowed.
        }
        else
        {
            throw new InvalidOperationException("Unsupported subscription change type.");
        }

        await EnsureNoActiveRequestAsync(guildId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var request = new PlanUpgradeRequest
        {
            GuildId = guildId,
            ChangeType = resolvedChangeType,
            RequestedPlanId = requestedPlan.Id,
            CurrentPlanId = currentSubscription.SubscriptionPlanId,
            RequestedByUserId = requestedByUserId,
            DurationMonths = durationMonths,
            RequestedPlanMonthlyPrice = requestedPlan.MonthlyPrice,
            EstimatedTotalAmount = requestedPlan.MonthlyPrice * durationMonths,
            Status = PlanUpgradeRequestStatus.Requested,
            RequestExpiresAt = now.AddDays(ManualBillingDefaults.RequestExpiryDays)
        };

        PlanUpgradeRequestWorkflow.EnsureTransition(
            request.Status,
            PlanUpgradeRequestStatus.PendingPayment);
        request.Status = PlanUpgradeRequestStatus.PendingPayment;

        _dbContext.PlanUpgradeRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapGuildRequestAsync(request.Id, cancellationToken);
    }

    public async Task<PlanUpgradeRequestDto?> GetCurrentChangeRequestAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, ownerDiscordUserId, cancellationToken))
        {
            return null;
        }

        await ApplyExpiryToGuildRequestsAsync(guildId, cancellationToken);

        var requestId = await _dbContext.PlanUpgradeRequests
            .AsNoTracking()
            .Where(r => r.GuildId == guildId && PlanUpgradeRequestWorkflow.ActiveStatuses.Contains(r.Status))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (requestId == Guid.Empty)
        {
            return null;
        }

        return await MapGuildRequestAsync(requestId, cancellationToken);
    }

    public async Task<GuildSubscriptionStatusDto?> GetSubscriptionStatusAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, ownerDiscordUserId, cancellationToken))
        {
            return null;
        }

        var subscription = await _subscriptionService.GetGuildSubscriptionAsync(
            guildId,
            ownerDiscordUserId,
            cancellationToken);

        if (subscription is null)
        {
            return null;
        }

        var currentChange = await GetCurrentChangeRequestAsync(
            guildId,
            ownerDiscordUserId,
            cancellationToken);

        return new GuildSubscriptionStatusDto
        {
            Subscription = subscription,
            CurrentChange = currentChange
        };
    }

    public async Task<PlanUpgradeRequestDto?> SubmitPaymentReferenceAsync(
        Guid guildId,
        Guid requestId,
        string ownerDiscordUserId,
        string paymentReference,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, ownerDiscordUserId, cancellationToken))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(paymentReference))
        {
            throw new InvalidOperationException("Payment reference is required.");
        }

        var trimmedReference = paymentReference.Trim();
        if (trimmedReference.Length > 500)
        {
            throw new InvalidOperationException("Payment reference must be 500 characters or fewer.");
        }

        var request = await _dbContext.PlanUpgradeRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.GuildId == guildId, cancellationToken);

        if (request is null)
        {
            return null;
        }

        await ApplyExpiryIfNeededAsync(request, cancellationToken);

        if (PlanUpgradeRequestWorkflow.IsTerminal(request.Status))
        {
            throw new InvalidOperationException(
                $"Payment reference cannot be submitted when the subscription change is '{request.Status}'.");
        }

        if (request.Status == PlanUpgradeRequestStatus.Expired
            || (request.RequestExpiresAt is not null && request.RequestExpiresAt <= DateTimeOffset.UtcNow))
        {
            throw new InvalidOperationException("This subscription change request has expired.");
        }

        if (!string.IsNullOrWhiteSpace(request.PaymentReference))
        {
            throw new InvalidOperationException("Payment reference has already been submitted.");
        }

        PlanUpgradeRequestWorkflow.EnsureCanSubmitPayment(request.Status);

        var now = DateTimeOffset.UtcNow;

        PlanUpgradeRequestWorkflow.EnsureTransition(
            request.Status,
            PlanUpgradeRequestStatus.PaymentSubmitted);
        request.Status = PlanUpgradeRequestStatus.PaymentSubmitted;
        request.PaymentReference = trimmedReference;
        request.PaymentSubmittedAt = now;

        PlanUpgradeRequestWorkflow.EnsureTransition(
            request.Status,
            PlanUpgradeRequestStatus.UnderReview);
        request.Status = PlanUpgradeRequestStatus.UnderReview;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapGuildRequestAsync(requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<PlanUpgradeRequestDto>> GetGuildRequestsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, ownerDiscordUserId, cancellationToken))
        {
            return [];
        }

        await ApplyExpiryToGuildRequestsAsync(guildId, cancellationToken);

        var requests = await _dbContext.PlanUpgradeRequests
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var result = new List<PlanUpgradeRequestDto>();
        foreach (var id in requests)
        {
            var mapped = await MapGuildRequestAsync(id, cancellationToken);
            if (mapped is not null)
            {
                result.Add(mapped);
            }
        }

        return result;
    }

    public async Task<IReadOnlyList<AdminPlanUpgradeRequestDto>> GetAllRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        await ApplyExpiryToAllRequestsAsync(cancellationToken);

        var ids = await _dbContext.PlanUpgradeRequests
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var result = new List<AdminPlanUpgradeRequestDto>();
        foreach (var id in ids)
        {
            var mapped = await MapAdminRequestAsync(id, cancellationToken);
            if (mapped is not null)
            {
                result.Add(mapped);
            }
        }

        return result;
    }

    public async Task<PlanUpgradeRequestDto?> CancelRequestAsync(
        Guid guildId,
        Guid requestId,
        string ownerDiscordUserId,
        Guid cancelledByUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.IsOwnerAsync(guildId, ownerDiscordUserId, cancellationToken))
        {
            return null;
        }

        var request = await _dbContext.PlanUpgradeRequests
            .FirstOrDefaultAsync(r => r.Id == requestId && r.GuildId == guildId, cancellationToken);

        if (request is null)
        {
            return null;
        }

        await ApplyExpiryIfNeededAsync(request, cancellationToken);
        CancelRequest(request, cancelledByUserId, adminNote: null);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapGuildRequestAsync(requestId, cancellationToken);
    }

    public async Task<AdminPlanUpgradeRequestDto?> CancelRequestAsAdminAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.PlanUpgradeRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return null;
        }

        await ApplyExpiryIfNeededAsync(request, cancellationToken);
        CancelRequest(request, adminUserId, adminNote);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAdminRequestAsync(requestId, cancellationToken);
    }

    public async Task<AdminPlanUpgradeRequestDto?> ApproveAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
        string? adminOverrideReason,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.PlanUpgradeRequests
            .Include(r => r.RequestedPlan)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return null;
        }

        await ApplyExpiryIfNeededAsync(request, cancellationToken);

        var hasOverride = !string.IsNullOrWhiteSpace(adminOverrideReason);
        PlanUpgradeRequestWorkflow.EnsureCanApprove(request.Status, hasOverride);

        if (request.Status == PlanUpgradeRequestStatus.PendingPayment && hasOverride)
        {
            request.AdminOverrideReason = adminOverrideReason!.Trim();
        }

        if (hasOverride)
        {
            _logger.LogWarning(
                "Admin override used for upgrade request {RequestId} on guild {GuildId}. Reason: {Reason}",
                request.Id,
                request.GuildId,
                request.AdminOverrideReason ?? adminOverrideReason);
        }

        var updated = await _subscriptionService.ActivateSubscriptionFromRequestAsync(
            request.GuildId,
            request.RequestedPlanId,
            request.DurationMonths,
            request.Id,
            cancellationToken);

        if (updated is null)
        {
            return null;
        }

        PlanUpgradeRequestWorkflow.EnsureTransition(request.Status, PlanUpgradeRequestStatus.Approved);
        request.Status = PlanUpgradeRequestStatus.Approved;
        request.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.ReviewedByAdminId = adminUserId;

        PlanUpgradeRequestWorkflow.EnsureTransition(request.Status, PlanUpgradeRequestStatus.Activated);
        request.Status = PlanUpgradeRequestStatus.Activated;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAdminRequestAsync(requestId, cancellationToken);
    }

    public async Task<AdminPlanUpgradeRequestDto?> RejectAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.PlanUpgradeRequests
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null)
        {
            return null;
        }

        await ApplyExpiryIfNeededAsync(request, cancellationToken);
        PlanUpgradeRequestWorkflow.EnsureCanReject(request.Status);

        if (string.IsNullOrWhiteSpace(adminNote))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        PlanUpgradeRequestWorkflow.EnsureTransition(request.Status, PlanUpgradeRequestStatus.Rejected);
        request.Status = PlanUpgradeRequestStatus.Rejected;
        request.AdminNote = adminNote.Trim();
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.ReviewedByAdminId = adminUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAdminRequestAsync(requestId, cancellationToken);
    }

    private async Task EnsureNoActiveRequestAsync(Guid guildId, CancellationToken cancellationToken)
    {
        var activeRequests = await _dbContext.PlanUpgradeRequests
            .Where(r => r.GuildId == guildId)
            .ToListAsync(cancellationToken);

        foreach (var existing in activeRequests)
        {
            await ApplyExpiryIfNeededAsync(existing, cancellationToken);
        }

        var hasActive = activeRequests.Any(r => PlanUpgradeRequestWorkflow.IsActive(r.Status));
        if (hasActive)
        {
            throw new InvalidOperationException("An active upgrade request already exists for this guild.");
        }
    }

    private static void CancelRequest(
        PlanUpgradeRequest request,
        Guid cancelledByUserId,
        string? adminNote)
    {
        PlanUpgradeRequestWorkflow.EnsureCanCancel(request.Status);
        PlanUpgradeRequestWorkflow.EnsureTransition(request.Status, PlanUpgradeRequestStatus.Cancelled);

        request.Status = PlanUpgradeRequestStatus.Cancelled;
        request.CancelledAt = DateTimeOffset.UtcNow;
        request.CancelledByUserId = cancelledByUserId;

        if (!string.IsNullOrWhiteSpace(adminNote))
        {
            request.AdminNote = adminNote.Trim();
        }
    }

    private async Task ApplyExpiryToGuildRequestsAsync(Guid guildId, CancellationToken cancellationToken)
    {
        var requests = await _dbContext.PlanUpgradeRequests
            .Where(r => r.GuildId == guildId)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var request in requests)
        {
            if (await ApplyExpiryIfNeededAsync(request, cancellationToken))
            {
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ApplyExpiryToAllRequestsAsync(CancellationToken cancellationToken)
    {
        var requests = await _dbContext.PlanUpgradeRequests.ToListAsync(cancellationToken);

        var changed = false;
        foreach (var request in requests)
        {
            if (await ApplyExpiryIfNeededAsync(request, cancellationToken))
            {
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private Task<bool> ApplyExpiryIfNeededAsync(
        PlanUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!PlanUpgradeRequestWorkflow.CanExpire(request.Status))
        {
            return Task.FromResult(false);
        }

        if (request.RequestExpiresAt is null || request.RequestExpiresAt > DateTimeOffset.UtcNow)
        {
            return Task.FromResult(false);
        }

        PlanUpgradeRequestWorkflow.EnsureTransition(request.Status, PlanUpgradeRequestStatus.Expired);
        request.Status = PlanUpgradeRequestStatus.Expired;

        return Task.FromResult(true);
    }

    private async Task<PlanUpgradeRequestDto?> MapGuildRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PlanUpgradeRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new PlanUpgradeRequestDto
            {
                Id = r.Id,
                GuildId = r.GuildId,
                ChangeType = r.ChangeType,
                RequestedPlanKey = r.RequestedPlan.Key,
                RequestedPlanName = r.RequestedPlan.Name,
                RequestedPlanMonthlyPrice = r.RequestedPlanMonthlyPrice,
                EstimatedTotalPrice = r.EstimatedTotalAmount,
                CurrentPlanKey = r.CurrentPlan.Key,
                CurrentPlanName = r.CurrentPlan.Name,
                RequestedByUsername = r.RequestedByUser.Username,
                DurationMonths = r.DurationMonths,
                Status = r.Status,
                PaymentReference = r.PaymentReference,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                PaymentSubmittedAt = r.PaymentSubmittedAt,
                RequestExpiresAt = r.RequestExpiresAt,
                EstimatedExpiresAtIfApprovedToday = DateTimeOffset.UtcNow.AddMonths(r.DurationMonths)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AdminPlanUpgradeRequestDto?> MapAdminRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.PlanUpgradeRequests
            .AsNoTracking()
            .Where(r => r.Id == requestId)
            .Select(r => new AdminPlanUpgradeRequestDto
            {
                Id = r.Id,
                GuildId = r.GuildId,
                GuildName = r.Guild.Name,
                ChangeType = r.ChangeType,
                CurrentPlanKey = r.CurrentPlan.Key,
                CurrentPlanName = r.CurrentPlan.Name,
                RequestedPlanKey = r.RequestedPlan.Key,
                RequestedPlanName = r.RequestedPlan.Name,
                RequestedPlanMonthlyPrice = r.RequestedPlanMonthlyPrice,
                EstimatedTotalPrice = r.EstimatedTotalAmount,
                RequestedByUsername = r.RequestedByUser.Username,
                RequestedByDiscordUserId = r.RequestedByUser.DiscordUserId,
                DurationMonths = r.DurationMonths,
                Status = r.Status,
                PaymentReference = r.PaymentReference,
                AdminNote = r.AdminNote,
                AdminOverrideReason = r.AdminOverrideReason,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                PaymentSubmittedAt = r.PaymentSubmittedAt,
                RequestExpiresAt = r.RequestExpiresAt,
                EstimatedExpiresAtIfApprovedToday = DateTimeOffset.UtcNow.AddMonths(r.DurationMonths)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
