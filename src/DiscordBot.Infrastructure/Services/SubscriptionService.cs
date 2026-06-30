using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
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
        var guild = await FindOwnedGuildAsync(guildId, ownerDiscordUserId, cancellationToken);
        if (guild is null)
        {
            return null;
        }

        await EnsureGuildSubscriptionAsync(guildId, cancellationToken);

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
        await _dbContext.SaveChangesAsync(cancellationToken);

        await DisableModulesOutsidePlanAsync(guildId, plan.AllowedModulesJson, cancellationToken);

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

        var freePlan = await _dbContext.SubscriptionPlans
            .FirstOrDefaultAsync(p => p.Key == PlanKeys.Free && p.IsActive, cancellationToken);

        if (freePlan is null)
        {
            return;
        }

        _dbContext.GuildSubscriptions.Add(new GuildSubscription
        {
            GuildId = guildId,
            SubscriptionPlanId = freePlan.Id
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

        return await _dbContext.GuildSubscriptions
            .AsNoTracking()
            .Where(gs => gs.GuildId == guildId)
            .Select(gs => gs.SubscriptionPlan.AllowedModulesJson)
            .FirstOrDefaultAsync(cancellationToken);
    }

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

        return new GuildSubscriptionDto
        {
            GuildId = guildId,
            PlanKey = subscription.SubscriptionPlan.Key,
            PlanName = subscription.SubscriptionPlan.Name,
            PlanDescription = subscription.SubscriptionPlan.Description,
            AllowedModules = PlanModulesExtensions.ParseAllowedModules(subscription.SubscriptionPlan.AllowedModulesJson)
        };
    }

    private static SubscriptionPlanDto MapPlan(SubscriptionPlan plan) =>
        new()
        {
            Key = plan.Key,
            Name = plan.Name,
            Description = plan.Description,
            AllowedModules = PlanModulesExtensions.ParseAllowedModules(plan.AllowedModulesJson),
            IsActive = plan.IsActive
        };

    private async Task<Guild?> FindOwnedGuildAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);
    }
}
