using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Domain.Extensions;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface ISubscriptionService
{
    Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(CancellationToken cancellationToken = default);

    Task<GuildSubscriptionDto?> GetGuildSubscriptionAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<GuildSubscriptionDto?> UpdateGuildSubscriptionAsync(
        Guid guildId,
        string ownerDiscordUserId,
        string planKey,
        CancellationToken cancellationToken = default);

    Task<GuildSubscriptionDto?> UpdateGuildSubscriptionAsAdminAsync(
        Guid guildId,
        string planKey,
        CancellationToken cancellationToken = default);

    Task<GuildSubscriptionDto?> ActivateSubscriptionFromRequestAsync(
        Guid guildId,
        Guid planId,
        int durationMonths,
        Guid approvedRequestId,
        CancellationToken cancellationToken = default);

    Task<GuildSubscriptionDto?> ExtendSubscriptionAsync(
        Guid guildId,
        int months,
        CancellationToken cancellationToken = default);

    Task<GuildSubscriptionDto?> CancelSubscriptionAsync(
        Guid guildId,
        CancellationToken cancellationToken = default);

    Task EnsureGuildSubscriptionAsync(Guid guildId, CancellationToken cancellationToken = default);

    Task<bool> IsModuleAllowedForGuildAsync(
        Guid guildId,
        string moduleKey,
        CancellationToken cancellationToken = default);

    Task<bool> IsModuleAllowedForDiscordGuildAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAllowedModuleKeysForGuildAsync(
        Guid guildId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminSubscriptionPlanDto>> GetAllPlansForAdminAsync(
        CancellationToken cancellationToken = default);

    Task<AdminSubscriptionPlanDto?> CreatePlanAsync(
        CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<AdminSubscriptionPlanDto?> UpdatePlanAsync(
        Guid planId,
        UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeletePlanAsync(Guid planId, CancellationToken cancellationToken = default);
}

public class SubscriptionService : ISubscriptionService
{
    private readonly AppDbContext _dbContext;

    public SubscriptionService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SubscriptionPlanDto>> GetPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return plans.Select(MapPlan).ToList();
    }

    public async Task<GuildSubscriptionDto?> GetGuildSubscriptionAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var isOwner = await _dbContext.Guilds
            .AsNoTracking()
            .AnyAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);

        if (!isOwner)
        {
            return null;
        }

        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);
        await GetSubscriptionAndApplyExpirationAsync(guildId, cancellationToken);

        return await BuildGuildSubscriptionDtoAsync(guildId, cancellationToken);
    }

    public async Task<GuildSubscriptionDto?> UpdateGuildSubscriptionAsync(
        Guid guildId,
        string ownerDiscordUserId,
        string planKey,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var plan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Key == planKey && p.IsActive, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);

        var subscription = await _dbContext.GuildSubscriptions
            .FirstAsync(gs => gs.GuildId == guildId, cancellationToken);

        subscription.SubscriptionPlanId = plan.Id;
        subscription.Status = GuildSubscriptionStatus.Active;
        subscription.StartedAt = DateTimeOffset.UtcNow;
        subscription.ExpiresAt = null;
        subscription.ApprovedRequestId = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await DisableModulesOutsidePlanAsync(guildId, plan.AllowedModulesJson, cancellationToken);

        return await BuildGuildSubscriptionDtoAsync(guildId, cancellationToken);
    }

    public async Task<GuildSubscriptionDto?> UpdateGuildSubscriptionAsAdminAsync(
        Guid guildId,
        string planKey,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var plan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Key == planKey && p.IsActive, cancellationToken);

        if (plan is null)
        {
            return null;
        }

        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);

        var subscription = await _dbContext.GuildSubscriptions
            .FirstAsync(gs => gs.GuildId == guildId, cancellationToken);

        subscription.SubscriptionPlanId = plan.Id;
        subscription.Status = GuildSubscriptionStatus.Active;
        subscription.StartedAt = plan.Key == PlanKeys.Free ? null : DateTimeOffset.UtcNow;
        subscription.ExpiresAt = null;
        subscription.ApprovedRequestId = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await DisableModulesOutsidePlanAsync(guildId, plan.AllowedModulesJson, cancellationToken);

        return await BuildGuildSubscriptionDtoAsync(guildId, cancellationToken);
    }

    public async Task<GuildSubscriptionDto?> ActivateSubscriptionFromRequestAsync(
        Guid guildId,
        Guid planId,
        int durationMonths,
        Guid approvedRequestId,
        CancellationToken cancellationToken = default)
    {
        if (!SubscriptionDurations.IsValid(durationMonths))
        {
            return null;
        }

        var plan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, cancellationToken);

        if (plan is null || plan.Key == PlanKeys.Free)
        {
            return null;
        }

        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var subscription = await _dbContext.GuildSubscriptions
            .FirstAsync(gs => gs.GuildId == guildId, cancellationToken);

        subscription.SubscriptionPlanId = plan.Id;
        subscription.Status = GuildSubscriptionStatus.Active;
        subscription.StartedAt = now;
        subscription.ExpiresAt = now.AddMonths(durationMonths);
        subscription.ApprovedRequestId = approvedRequestId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await DisableModulesOutsidePlanAsync(guildId, plan.AllowedModulesJson, cancellationToken);

        return await BuildGuildSubscriptionDtoAsync(guildId, cancellationToken);
    }

    public async Task<GuildSubscriptionDto?> ExtendSubscriptionAsync(
        Guid guildId,
        int months,
        CancellationToken cancellationToken = default)
    {
        if (!SubscriptionDurations.IsValid(months))
        {
            return null;
        }

        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);
        var subscription = await GetSubscriptionAndApplyExpirationAsync(guildId, cancellationToken);

        if (subscription.SubscriptionPlan.Key == PlanKeys.Free)
        {
            return null;
        }

        var baseDate = subscription.ExpiresAt ?? DateTimeOffset.UtcNow;
        if (baseDate < DateTimeOffset.UtcNow)
        {
            baseDate = DateTimeOffset.UtcNow;
        }

        subscription.Status = GuildSubscriptionStatus.Active;
        subscription.ExpiresAt = baseDate.AddMonths(months);
        if (!subscription.StartedAt.HasValue)
        {
            subscription.StartedAt = DateTimeOffset.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await BuildGuildSubscriptionDtoAsync(guildId, cancellationToken);
    }

    public async Task<GuildSubscriptionDto?> CancelSubscriptionAsync(
        Guid guildId,
        CancellationToken cancellationToken = default)
    {
        var freePlan = await GetFreePlanAsync(cancellationToken);
        if (freePlan is null)
        {
            return null;
        }

        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);
        var subscription = await _dbContext.GuildSubscriptions
            .FirstAsync(gs => gs.GuildId == guildId, cancellationToken);

        subscription.SubscriptionPlanId = freePlan.Id;
        subscription.Status = GuildSubscriptionStatus.Cancelled;
        subscription.StartedAt = null;
        subscription.ExpiresAt = null;
        subscription.ApprovedRequestId = null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await DisableModulesOutsidePlanAsync(guildId, freePlan.AllowedModulesJson, cancellationToken);

        return await BuildGuildSubscriptionDtoAsync(guildId, cancellationToken);
    }

    public async Task EnsureGuildSubscriptionAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.GuildSubscriptions
            .AnyAsync(gs => gs.GuildId == guildId, cancellationToken);

        if (exists)
        {
            return;
        }

        var freePlan = await GetFreePlanAsync(cancellationToken);
        if (freePlan is null)
        {
            return;
        }

        _dbContext.GuildSubscriptions.Add(new GuildSubscription
        {
            GuildId = guildId,
            SubscriptionPlanId = freePlan.Id,
            Status = GuildSubscriptionStatus.Active
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsModuleAllowedForGuildAsync(
        Guid guildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var allowedJson = await GetAllowedModulesJsonAsync(guildId, cancellationToken);
        return allowedJson is not null && PlanModulesExtensions.AllowsModule(allowedJson, moduleKey);
    }

    public async Task<bool> IsModuleAllowedForDiscordGuildAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return false;
        }

        return await IsModuleAllowedForGuildAsync(guild.Id, moduleKey, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetAllowedModuleKeysForGuildAsync(
        Guid guildId,
        CancellationToken cancellationToken = default)
    {
        var allowedJson = await GetAllowedModulesJsonAsync(guildId, cancellationToken);
        if (allowedJson is null)
        {
            return [];
        }

        return PlanModulesExtensions.ParseAllowedModules(allowedJson);
    }

    private async Task<string?> GetAllowedModulesJsonAsync(Guid guildId, CancellationToken cancellationToken)
    {
        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);
        var subscription = await GetSubscriptionAndApplyExpirationAsync(guildId, cancellationToken);

        return subscription.SubscriptionPlan.AllowedModulesJson;
    }

    private async Task<GuildSubscription> GetSubscriptionAndApplyExpirationAsync(
        Guid guildId,
        CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.GuildSubscriptions
            .Include(gs => gs.SubscriptionPlan)
            .FirstAsync(gs => gs.GuildId == guildId, cancellationToken);

        await ApplyExpirationIfNeededAsync(subscription, cancellationToken);
        return subscription;
    }

    private async Task ApplyExpirationIfNeededAsync(
        GuildSubscription subscription,
        CancellationToken cancellationToken)
    {
        if (subscription.Status != GuildSubscriptionStatus.Active)
        {
            return;
        }

        if (!subscription.ExpiresAt.HasValue || subscription.ExpiresAt.Value > DateTimeOffset.UtcNow)
        {
            return;
        }

        var freePlan = await GetFreePlanAsync(cancellationToken);
        if (freePlan is null)
        {
            return;
        }

        subscription.Status = GuildSubscriptionStatus.Expired;
        subscription.SubscriptionPlanId = freePlan.Id;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await DisableModulesOutsidePlanAsync(subscription.GuildId, freePlan.AllowedModulesJson, cancellationToken);

        await _dbContext.Entry(subscription).Reference(gs => gs.SubscriptionPlan).LoadAsync(cancellationToken);
    }

    private async Task<SubscriptionPlan?> GetFreePlanAsync(CancellationToken cancellationToken) =>
        await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Key == PlanKeys.Free && p.IsActive, cancellationToken);

    private async Task DisableModulesOutsidePlanAsync(
        Guid guildId,
        string allowedModulesJson,
        CancellationToken cancellationToken)
    {
        if (PlanModulesExtensions.ParseAllowedModules(allowedModulesJson).Contains(PlanKeys.AllModulesToken))
        {
            return;
        }

        var guildModules = await _dbContext.GuildModules
            .Include(gm => gm.Module)
            .Where(gm => gm.GuildId == guildId && gm.IsEnabled)
            .ToListAsync(cancellationToken);

        var changed = false;
        foreach (var guildModule in guildModules)
        {
            if (!PlanModulesExtensions.AllowsModule(allowedModulesJson, guildModule.Module.Key))
            {
                guildModule.IsEnabled = false;
                changed = true;
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<GuildSubscriptionDto?> BuildGuildSubscriptionDtoAsync(
        Guid guildId,
        CancellationToken cancellationToken)
    {
        var subscription = await _dbContext.GuildSubscriptions
            .AsNoTracking()
            .Include(gs => gs.SubscriptionPlan)
            .FirstOrDefaultAsync(gs => gs.GuildId == guildId, cancellationToken);

        if (subscription is null)
        {
            return null;
        }

        var isExpired = subscription.Status is GuildSubscriptionStatus.Expired or GuildSubscriptionStatus.Cancelled;

        return new GuildSubscriptionDto
        {
            GuildId = guildId,
            PlanKey = subscription.SubscriptionPlan.Key,
            PlanName = subscription.SubscriptionPlan.Name,
            PlanDescription = subscription.SubscriptionPlan.Description,
            AllowedModules = PlanModulesExtensions.ParseAllowedModules(subscription.SubscriptionPlan.AllowedModulesJson),
            Status = subscription.Status,
            StartedAt = subscription.StartedAt,
            ExpiresAt = subscription.ExpiresAt,
            IsExpired = isExpired
        };
    }

    public async Task<IReadOnlyList<AdminSubscriptionPlanDto>> GetAllPlansForAdminAsync(
        CancellationToken cancellationToken = default)
    {
        var plans = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .OrderBy(p => p.MonthlyPrice)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var subscriberCounts = await _dbContext.GuildSubscriptions
            .AsNoTracking()
            .GroupBy(gs => gs.SubscriptionPlanId)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count, cancellationToken);

        return plans
            .Select(plan => MapAdminPlan(plan, subscriberCounts.GetValueOrDefault(plan.Id)))
            .ToList();
    }

    public async Task<AdminSubscriptionPlanDto?> CreatePlanAsync(
        CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var key = NormalizePlanKey(request.Key);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("Plan key is required.");
        }

        if (await _dbContext.SubscriptionPlans.AnyAsync(p => p.Key == key, cancellationToken))
        {
            throw new InvalidOperationException("A plan with this key already exists.");
        }

        ValidatePlanRequest(request.Name, request.Description, request.MonthlyPrice, request.AllowedModules);

        var entity = new SubscriptionPlan
        {
            Key = key,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            AllowedModulesJson = PlanModulesExtensions.SerializeAllowedModules(request.AllowedModules),
            MonthlyPrice = request.MonthlyPrice,
            IsActive = request.IsActive
        };

        _dbContext.SubscriptionPlans.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapAdminPlan(entity, 0);
    }

    public async Task<AdminSubscriptionPlanDto?> UpdatePlanAsync(
        Guid planId,
        UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        ValidatePlanRequest(request.Name, request.Description, request.MonthlyPrice, request.AllowedModules);

        entity.Name = request.Name.Trim();
        entity.Description = request.Description.Trim();
        entity.AllowedModulesJson = PlanModulesExtensions.SerializeAllowedModules(request.AllowedModules);
        entity.MonthlyPrice = request.MonthlyPrice;
        entity.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var subscriberCount = await _dbContext.GuildSubscriptions
            .CountAsync(gs => gs.SubscriptionPlanId == planId, cancellationToken);

        return MapAdminPlan(entity, subscriberCount);
    }

    public async Task<bool> DeletePlanAsync(Guid planId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        if (entity.Key == PlanKeys.Free)
        {
            throw new InvalidOperationException("The free plan cannot be deleted.");
        }

        var inUse = await _dbContext.GuildSubscriptions.AnyAsync(
            gs => gs.SubscriptionPlanId == planId,
            cancellationToken);

        if (inUse)
        {
            throw new InvalidOperationException("This plan is assigned to one or more guilds. Suspend it instead of deleting.");
        }

        var referenced = await _dbContext.PlanUpgradeRequests.AnyAsync(
            r => r.RequestedPlanId == planId || r.CurrentPlanId == planId,
            cancellationToken);

        if (referenced)
        {
            throw new InvalidOperationException("This plan is referenced by upgrade requests and cannot be deleted.");
        }

        _dbContext.SubscriptionPlans.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizePlanKey(string key) =>
        key.Trim().ToLowerInvariant();

    private static void ValidatePlanRequest(
        string name,
        string description,
        decimal monthlyPrice,
        IReadOnlyList<string> allowedModules)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Plan name is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new InvalidOperationException("Plan description is required.");
        }

        if (monthlyPrice < 0)
        {
            throw new InvalidOperationException("Monthly price cannot be negative.");
        }

        if (allowedModules is null || allowedModules.Count == 0)
        {
            throw new InvalidOperationException("Select at least one allowed module.");
        }
    }

    private static AdminSubscriptionPlanDto MapAdminPlan(SubscriptionPlan plan, int subscriberCount) =>
        new()
        {
            Id = plan.Id,
            Key = plan.Key,
            Name = plan.Name,
            Description = plan.Description,
            AllowedModules = PlanModulesExtensions.ParseAllowedModules(plan.AllowedModulesJson),
            IsActive = plan.IsActive,
            MonthlyPrice = plan.MonthlyPrice,
            SubscriberCount = subscriberCount
        };

    private static SubscriptionPlanDto MapPlan(SubscriptionPlan plan) =>
        new()
        {
            Key = plan.Key,
            Name = plan.Name,
            Description = plan.Description,
            AllowedModules = PlanModulesExtensions.ParseAllowedModules(plan.AllowedModulesJson),
            IsActive = plan.IsActive,
            MonthlyPrice = plan.MonthlyPrice
        };
}
