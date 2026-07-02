using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IPlanUpgradeRequestService
{
    Task<PlanUpgradeRequestDto?> CreateRequestAsync(
        Guid guildId,
        string ownerDiscordUserId,
        Guid requestedByUserId,
        string planKey,
        int durationMonths,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PlanUpgradeRequestDto>> GetGuildRequestsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminPlanUpgradeRequestDto>> GetAllRequestsAsync(
        CancellationToken cancellationToken = default);

    Task<AdminPlanUpgradeRequestDto?> ApproveAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
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

    public PlanUpgradeRequestService(
        AppDbContext dbContext,
        IGuildAccessService guildAccessService,
        ISubscriptionService subscriptionService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
        _subscriptionService = subscriptionService;
    }

    public async Task<PlanUpgradeRequestDto?> CreateRequestAsync(
        Guid guildId,
        string ownerDiscordUserId,
        Guid requestedByUserId,
        string planKey,
        int durationMonths,
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

        var hasPending = await _dbContext.PlanUpgradeRequests
            .AnyAsync(
                r => r.GuildId == guildId && r.Status == PlanUpgradeRequestStatus.Pending,
                cancellationToken);

        if (hasPending)
        {
            throw new InvalidOperationException("A pending upgrade request already exists for this guild.");
        }

        var request = new PlanUpgradeRequest
        {
            GuildId = guildId,
            RequestedPlanId = requestedPlan.Id,
            CurrentPlanId = currentSubscription.SubscriptionPlanId,
            RequestedByUserId = requestedByUserId,
            DurationMonths = durationMonths,
            Status = PlanUpgradeRequestStatus.Pending
        };

        _dbContext.PlanUpgradeRequests.Add(request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapGuildRequestAsync(request.Id, cancellationToken);
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

    public async Task<AdminPlanUpgradeRequestDto?> ApproveAsync(
        Guid requestId,
        Guid adminUserId,
        string? adminNote,
        CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.PlanUpgradeRequests
            .Include(r => r.RequestedPlan)
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken);

        if (request is null || request.Status != PlanUpgradeRequestStatus.Pending)
        {
            return null;
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

        request.Status = PlanUpgradeRequestStatus.Approved;
        request.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.ReviewedByAdminId = adminUserId;

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

        if (request is null || request.Status != PlanUpgradeRequestStatus.Pending)
        {
            return null;
        }

        request.Status = PlanUpgradeRequestStatus.Rejected;
        request.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        request.ReviewedAt = DateTimeOffset.UtcNow;
        request.ReviewedByAdminId = adminUserId;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await MapAdminRequestAsync(requestId, cancellationToken);
    }

    private static DateTimeOffset CalculateEstimatedExpiry(int durationMonths) =>
        DateTimeOffset.UtcNow.AddMonths(durationMonths);

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
                RequestedPlanKey = r.RequestedPlan.Key,
                RequestedPlanName = r.RequestedPlan.Name,
                RequestedPlanMonthlyPrice = r.RequestedPlan.MonthlyPrice,
                EstimatedTotalPrice = r.RequestedPlan.MonthlyPrice * r.DurationMonths,
                CurrentPlanKey = r.CurrentPlan.Key,
                CurrentPlanName = r.CurrentPlan.Name,
                RequestedByUsername = r.RequestedByUser.Username,
                DurationMonths = r.DurationMonths,
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
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
                CurrentPlanKey = r.CurrentPlan.Key,
                CurrentPlanName = r.CurrentPlan.Name,
                RequestedPlanKey = r.RequestedPlan.Key,
                RequestedPlanName = r.RequestedPlan.Name,
                RequestedPlanMonthlyPrice = r.RequestedPlan.MonthlyPrice,
                EstimatedTotalPrice = r.RequestedPlan.MonthlyPrice * r.DurationMonths,
                RequestedByUsername = r.RequestedByUser.Username,
                RequestedByDiscordUserId = r.RequestedByUser.DiscordUserId,
                DurationMonths = r.DurationMonths,
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ReviewedAt = r.ReviewedAt,
                EstimatedExpiresAtIfApprovedToday = DateTimeOffset.UtcNow.AddMonths(r.DurationMonths)
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
