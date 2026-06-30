using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IModuleService
{
    Task<IReadOnlyList<GuildModuleDto>> GetGuildModulesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<ModuleUpdateResult> UpdateGuildModuleAsync(
        Guid guildId,
        string ownerDiscordUserId,
        string moduleKey,
        bool isEnabled,
        CancellationToken cancellationToken = default);

    Task<GuildModuleStatusDto?> GetModuleStatusAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default);

    Task<bool> IsModuleEnabledAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default);

    Task EnsureGuildModulesAsync(Guid guildId, CancellationToken cancellationToken = default);
}

public class ModuleService : IModuleService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogService _logService;
    private readonly ISubscriptionService _subscriptionService;

    public ModuleService(
        AppDbContext dbContext,
        ILogService logService,
        ISubscriptionService subscriptionService)
    {
        _dbContext = dbContext;
        _logService = logService;
        _subscriptionService = subscriptionService;
    }

    public async Task<IReadOnlyList<GuildModuleDto>> GetGuildModulesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await FindOwnedGuildAsync(guildId, ownerDiscordUserId, cancellationToken);
        if (guild is null)
        {
            return [];
        }

        await EnsureGuildModulesAsync(guildId, cancellationToken);

        var allowedModules = await _subscriptionService.GetAllowedModuleKeysForGuildAsync(guildId, cancellationToken);
        var allowsAll = allowedModules.Contains(PlanKeys.AllModulesToken);

        var modules = await _dbContext.GuildModules
            .AsNoTracking()
            .Include(gm => gm.Module)
            .Where(gm => gm.GuildId == guildId)
            .OrderBy(gm => gm.Module.Name)
            .ToListAsync(cancellationToken);

        return modules.Select(gm => MapToDto(
            gm,
            allowsAll || allowedModules.Contains(gm.Module.Key))).ToList();
    }

    public async Task<ModuleUpdateResult> UpdateGuildModuleAsync(
        Guid guildId,
        string ownerDiscordUserId,
        string moduleKey,
        bool isEnabled,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);

        if (guild is null)
        {
            return new ModuleUpdateResult();
        }

        await EnsureGuildModulesAsync(guildId, cancellationToken);

        if (isEnabled && !await _subscriptionService.IsModuleAllowedForGuildAsync(guildId, moduleKey, cancellationToken))
        {
            return new ModuleUpdateResult { ErrorCode = "MODULE_NOT_IN_PLAN" };
        }

        var guildModule = await _dbContext.GuildModules
            .Include(gm => gm.Module)
            .FirstOrDefaultAsync(
                gm => gm.GuildId == guildId && gm.Module.Key == moduleKey,
                cancellationToken);

        if (guildModule is null)
        {
            return new ModuleUpdateResult();
        }

        guildModule.IsEnabled = isEnabled;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = guild.DiscordGuildId,
            Type = Domain.Enums.LogEventType.ModuleChanged,
            Message = $"{guildModule.Module.Name} module {(isEnabled ? "enabled" : "disabled")}.",
            ActorDiscordUserId = ownerDiscordUserId,
            MetadataJson = LogService.BuildMetadataJson(new
            {
                moduleKey = guildModule.Module.Key,
                isEnabled
            })
        }, cancellationToken);

        var allowed = await _subscriptionService.IsModuleAllowedForGuildAsync(guildId, moduleKey, cancellationToken);

        return new ModuleUpdateResult
        {
            Module = MapToDto(guildModule, allowed)
        };
    }

    public async Task<GuildModuleStatusDto?> GetModuleStatusAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        await EnsureGuildModulesAsync(guild.Id, cancellationToken);

        var isEnabled = await _dbContext.GuildModules
            .AsNoTracking()
            .Where(gm => gm.GuildId == guild.Id && gm.Module.Key == moduleKey)
            .Select(gm => gm.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);

        var allowedByPlan = await _subscriptionService.IsModuleAllowedForGuildAsync(
            guild.Id,
            moduleKey,
            cancellationToken);

        return new GuildModuleStatusDto
        {
            Key = moduleKey,
            IsEnabled = isEnabled,
            AllowedByPlan = allowedByPlan,
            EffectiveEnabled = isEnabled && allowedByPlan
        };
    }

    public async Task<bool> IsModuleEnabledAsync(
        string discordGuildId,
        string moduleKey,
        CancellationToken cancellationToken = default)
    {
        var status = await GetModuleStatusAsync(discordGuildId, moduleKey, cancellationToken);
        return status is { IsEnabled: true, AllowedByPlan: true };
    }

    public async Task EnsureGuildModulesAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var modules = await _dbContext.Modules.AsNoTracking().ToListAsync(cancellationToken);
        if (modules.Count == 0)
        {
            return;
        }

        var existingModuleIds = await _dbContext.GuildModules
            .Where(gm => gm.GuildId == guildId)
            .Select(gm => gm.ModuleId)
            .ToListAsync(cancellationToken);

        var missing = modules.Where(m => !existingModuleIds.Contains(m.Id)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var module in missing)
        {
            _dbContext.GuildModules.Add(new GuildModule
            {
                GuildId = guildId,
                ModuleId = module.Id,
                IsEnabled = true
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static GuildModuleDto MapToDto(GuildModule guildModule, bool allowedByPlan) =>
        new()
        {
            Key = guildModule.Module.Key,
            Name = guildModule.Module.Name,
            Description = guildModule.Module.Description,
            IsEnabled = guildModule.IsEnabled,
            AllowedByPlan = allowedByPlan,
            EffectiveEnabled = guildModule.IsEnabled && allowedByPlan
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
