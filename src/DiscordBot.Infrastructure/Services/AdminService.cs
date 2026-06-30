using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Domain.Extensions;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IAdminService
{
    Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminGuildSummaryDto>> GetGuildsAsync(CancellationToken cancellationToken = default);

    Task<AdminGuildDetailDto?> GetGuildAsync(Guid guildId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
}

public class AdminService : IAdminService
{
    private readonly AppDbContext _dbContext;

    public AdminService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalGuilds = await _dbContext.Guilds.CountAsync(cancellationToken);
        var activeGuilds = await _dbContext.Guilds.CountAsync(g => g.IsActive, cancellationToken);
        var totalUsers = await _dbContext.Users.CountAsync(cancellationToken);
        var totalTickets = await _dbContext.Tickets.CountAsync(cancellationToken);
        var openTickets = await _dbContext.Tickets.CountAsync(t => t.Status == TicketStatus.Open, cancellationToken);

        var planCounts = await _dbContext.SubscriptionPlans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new AdminPlanCountDto
            {
                PlanKey = p.Key,
                PlanName = p.Name,
                Count = _dbContext.GuildSubscriptions.Count(gs => gs.SubscriptionPlanId == p.Id)
            })
            .ToListAsync(cancellationToken);

        var moduleUsageCounts = await _dbContext.Modules
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .Select(m => new AdminModuleUsageDto
            {
                ModuleKey = m.Key,
                ModuleName = m.Name,
                EnabledGuildCount = _dbContext.GuildModules.Count(gm => gm.ModuleId == m.Id && gm.IsEnabled)
            })
            .ToListAsync(cancellationToken);

        return new AdminStatsDto
        {
            TotalGuilds = totalGuilds,
            ActiveGuilds = activeGuilds,
            TotalUsers = totalUsers,
            TotalTickets = totalTickets,
            OpenTickets = openTickets,
            PlanCounts = planCounts,
            ModuleUsageCounts = moduleUsageCounts
        };
    }

    public async Task<IReadOnlyList<AdminGuildSummaryDto>> GetGuildsAsync(
        CancellationToken cancellationToken = default)
    {
        var guilds = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Subscription)
                .ThenInclude(s => s!.SubscriptionPlan)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        var guildIds = guilds.Select(g => g.Id).ToList();

        var enabledModuleCounts = await _dbContext.GuildModules
            .AsNoTracking()
            .Where(gm => guildIds.Contains(gm.GuildId) && gm.IsEnabled)
            .GroupBy(gm => gm.GuildId)
            .Select(g => new { GuildId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GuildId, x => x.Count, cancellationToken);

        var ticketCounts = await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => guildIds.Contains(t.GuildId))
            .GroupBy(t => t.GuildId)
            .Select(g => new { GuildId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GuildId, x => x.Count, cancellationToken);

        return guilds.Select(g => MapSummary(
            g,
            enabledModuleCounts.GetValueOrDefault(g.Id),
            ticketCounts.GetValueOrDefault(g.Id))).ToList();
    }

    public async Task<AdminGuildDetailDto?> GetGuildAsync(
        Guid guildId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Subscription)
                .ThenInclude(s => s!.SubscriptionPlan)
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var enabledModulesCount = await _dbContext.GuildModules
            .CountAsync(gm => gm.GuildId == guildId && gm.IsEnabled, cancellationToken);

        var ticketsCount = await _dbContext.Tickets
            .CountAsync(t => t.GuildId == guildId, cancellationToken);

        var openTicketsCount = await _dbContext.Tickets
            .CountAsync(t => t.GuildId == guildId && t.Status == TicketStatus.Open, cancellationToken);

        var summary = MapSummary(guild, enabledModulesCount, ticketsCount);
        var allowedModules = guild.Subscription?.SubscriptionPlan is not null
            ? PlanModulesExtensions.ParseAllowedModules(guild.Subscription.SubscriptionPlan.AllowedModulesJson)
            : [];

        return new AdminGuildDetailDto
        {
            Id = summary.Id,
            DiscordGuildId = summary.DiscordGuildId,
            Name = summary.Name,
            OwnerDiscordUserId = summary.OwnerDiscordUserId,
            PlanKey = summary.PlanKey,
            PlanName = summary.PlanName,
            EnabledModulesCount = summary.EnabledModulesCount,
            TicketsCount = summary.TicketsCount,
            ResourcesSyncedAt = summary.ResourcesSyncedAt,
            IsActive = summary.IsActive,
            AllowedModules = allowedModules,
            OpenTicketsCount = openTicketsCount,
            CreatedAt = guild.CreatedAt
        };
    }

    public async Task<IReadOnlyList<AdminUserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .OrderByDescending(u => u.LastLoginAt ?? u.CreatedAt)
            .Select(u => new AdminUserDto
            {
                Id = u.Id,
                DiscordUserId = u.DiscordUserId,
                Username = u.Username,
                GlobalName = u.GlobalName,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private static AdminGuildSummaryDto MapSummary(Guild guild, int enabledModulesCount, int ticketsCount)
    {
        var plan = guild.Subscription?.SubscriptionPlan;

        return new AdminGuildSummaryDto
        {
            Id = guild.Id,
            DiscordGuildId = guild.DiscordGuildId,
            Name = guild.Name,
            OwnerDiscordUserId = guild.OwnerDiscordUserId,
            PlanKey = plan?.Key ?? string.Empty,
            PlanName = plan?.Name ?? "Unknown",
            EnabledModulesCount = enabledModulesCount,
            TicketsCount = ticketsCount,
            ResourcesSyncedAt = guild.ResourcesSyncedAt,
            IsActive = guild.IsActive
        };
    }
}
