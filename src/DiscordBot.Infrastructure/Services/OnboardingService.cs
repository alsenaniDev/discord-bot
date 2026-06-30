using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DiscordBot.Infrastructure.Services;

public interface IOnboardingService
{
    Task<OnboardingStatusDto> GetStatusAsync(string discordUserId, CancellationToken cancellationToken = default);

    Task<OnboardingChecklistDto?> GetGuildChecklistAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class OnboardingService : IOnboardingService
{
    private const long DefaultBotPermissions = 268513278;

    private readonly AppDbContext _dbContext;
    private readonly DiscordOptions _discordOptions;

    public OnboardingService(AppDbContext dbContext, IOptions<DiscordOptions> discordOptions)
    {
        _dbContext = dbContext;
        _discordOptions = discordOptions.Value;
    }

    public async Task<OnboardingStatusDto> GetStatusAsync(
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var guilds = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Settings)
            .Include(g => g.Subscription)
            .Where(g => g.OwnerDiscordUserId == discordUserId && g.IsActive)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

        var guildIds = guilds.Select(g => g.Id).ToList();

        var enabledModuleGuildIds = await _dbContext.GuildModules
            .AsNoTracking()
            .Where(gm => guildIds.Contains(gm.GuildId) && gm.IsEnabled)
            .Select(gm => gm.GuildId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var resourceCounts = await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => guildIds.Contains(g.Id))
            .Select(g => new
            {
                g.Id,
                ChannelCount = g.Channels.Count,
                RoleCount = g.Roles.Count
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var guildOnboarding = guilds.Select(g =>
        {
            var counts = resourceCounts.GetValueOrDefault(g.Id);
            return new GuildOnboardingDto
            {
                GuildId = g.Id,
                Name = g.Name,
                IconUrl = g.IconUrl,
                Checklist = BuildChecklist(
                    g.IsActive,
                    g.ResourcesSyncedAt,
                    counts?.ChannelCount ?? 0,
                    counts?.RoleCount ?? 0,
                    g.Subscription is not null,
                    enabledModuleGuildIds.Contains(g.Id),
                    g.Settings)
            };
        }).ToList();

        return new OnboardingStatusDto
        {
            HasGuilds = guildOnboarding.Count > 0,
            BotInviteUrl = BuildBotInviteUrl(_discordOptions.ClientId),
            DashboardUrl = _discordOptions.DashboardUrl.TrimEnd('/'),
            Guilds = guildOnboarding
        };
    }

    public async Task<OnboardingChecklistDto?> GetGuildChecklistAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Settings)
            .Include(g => g.Subscription)
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == discordUserId && g.IsActive,
                cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var channelCount = await _dbContext.DiscordChannels
            .CountAsync(c => c.GuildId == guildId, cancellationToken);

        var roleCount = await _dbContext.DiscordRoles
            .CountAsync(r => r.GuildId == guildId, cancellationToken);

        var hasEnabledModule = await _dbContext.GuildModules
            .AnyAsync(gm => gm.GuildId == guildId && gm.IsEnabled, cancellationToken);

        return BuildChecklist(
            guild.IsActive,
            guild.ResourcesSyncedAt,
            channelCount,
            roleCount,
            guild.Subscription is not null,
            hasEnabledModule,
            guild.Settings);
    }

    public static string BuildBotInviteUrl(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return string.Empty;
        }

        return
            $"https://discord.com/oauth2/authorize?client_id={Uri.EscapeDataString(clientId.Trim())}" +
            $"&permissions={DefaultBotPermissions}&scope=bot%20applications.commands";
    }

    internal static OnboardingChecklistDto BuildChecklist(
        bool isActive,
        DateTimeOffset? resourcesSyncedAt,
        int channelCount,
        int roleCount,
        bool hasSubscription,
        bool hasEnabledModule,
        GuildSettings? settings)
    {
        var botInvited = isActive;
        var resourcesSynced = resourcesSyncedAt.HasValue && (channelCount > 0 || roleCount > 0);
        var planSelected = hasSubscription;
        var modulesEnabled = hasEnabledModule;
        var welcomeConfigured = settings?.WelcomeEnabled == true
            && !string.IsNullOrWhiteSpace(settings.WelcomeChannelId);
        var ticketsConfigured = settings?.TicketsEnabled == true
            && !string.IsNullOrWhiteSpace(settings.TicketCategoryId);

        var completed = new[]
        {
            botInvited,
            resourcesSynced,
            planSelected,
            modulesEnabled,
            welcomeConfigured,
            ticketsConfigured
        }.Count(x => x);

        const int total = 6;

        return new OnboardingChecklistDto
        {
            BotInvited = botInvited,
            ResourcesSynced = resourcesSynced,
            PlanSelected = planSelected,
            ModulesEnabled = modulesEnabled,
            WelcomeConfigured = welcomeConfigured,
            TicketsConfigured = ticketsConfigured,
            CompletedCount = completed,
            TotalCount = total,
            ProgressPercent = (int)Math.Round(completed * 100.0 / total)
        };
    }
}
