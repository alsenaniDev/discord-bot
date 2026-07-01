using DiscordBot.Domain.Enums;
using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildService
{
    Task<IReadOnlyList<GuildSummaryDto>> GetAccessibleGuildsAsync(
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<GuildSettingsDto?> GetSettingsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<GuildSettingsDto?> UpdateSettingsAsync(
        Guid guildId,
        string discordUserId,
        UpdateGuildSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<RegisterGuildResponse> RegisterGuildAsync(
        RegisterGuildRequest request,
        CancellationToken cancellationToken = default);

    Task<GuildSettingsDto?> GetSettingsByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default);

    Task<GuildOverviewDto?> GetOverviewAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class GuildService : IGuildService
{
    private readonly AppDbContext _dbContext;
    private readonly IModuleService _moduleService;
    private readonly ILogService _logService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IOnboardingService _onboardingService;
    private readonly IGuildAccessService _guildAccessService;

    public GuildService(
        AppDbContext dbContext,
        IModuleService moduleService,
        ILogService logService,
        ISubscriptionService subscriptionService,
        IOnboardingService onboardingService,
        IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _moduleService = moduleService;
        _logService = logService;
        _subscriptionService = subscriptionService;
        _onboardingService = onboardingService;
        _guildAccessService = guildAccessService;
    }

    public async Task<IReadOnlyList<GuildSummaryDto>> GetAccessibleGuildsAsync(
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var ownedGuilds = await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.OwnerDiscordUserId == discordUserId && g.IsActive)
            .Select(g => new GuildSummaryDto
            {
                Id = g.Id,
                DiscordGuildId = g.DiscordGuildId,
                Name = g.Name,
                IconUrl = g.IconUrl,
                IsActive = g.IsActive,
                IsOwner = true,
                StaffRole = null
            })
            .ToListAsync(cancellationToken);

        var staffGuilds = new List<GuildSummaryDto>();
        var memberRecords = await _dbContext.DiscordGuildMembers
            .AsNoTracking()
            .Where(m => m.DiscordUserId == discordUserId)
            .Where(m => m.Guild.IsActive)
            .Where(m => m.Guild.OwnerDiscordUserId != discordUserId)
            .Select(m => new
            {
                m.GuildId,
                m.Guild.DiscordGuildId,
                m.Guild.Name,
                m.Guild.IconUrl,
                m.Guild.IsActive,
                m.DiscordRoleIdsJson
            })
            .ToListAsync(cancellationToken);

        if (memberRecords.Count > 0)
        {
            var guildIds = memberRecords.Select(m => m.GuildId).Distinct().ToList();
            var permissionRoles = await _dbContext.GuildPermissionRoles
                .AsNoTracking()
                .Where(r => guildIds.Contains(r.GuildId))
                .ToListAsync(cancellationToken);

            foreach (var member in memberRecords)
            {
                var userRoleIds = GuildPermissionResolver.ParseRoleIds(member.DiscordRoleIdsJson);
                var matched = permissionRoles
                    .Where(r => r.GuildId == member.GuildId && userRoleIds.Contains(r.DiscordRoleId))
                    .ToList();

                if (matched.Count == 0 || matched.All(r => r.Permissions == GuildPermissions.None))
                {
                    continue;
                }

                staffGuilds.Add(new GuildSummaryDto
                {
                    Id = member.GuildId,
                    DiscordGuildId = member.DiscordGuildId,
                    Name = member.Name,
                    IconUrl = member.IconUrl,
                    IsActive = member.IsActive,
                    IsOwner = false,
                    StaffRole = string.Join(", ", matched.Select(r => r.Name))
                });
            }
        }

        return ownedGuilds
            .Concat(staffGuilds)
            .OrderBy(g => g.Name)
            .ToList();
    }

    public async Task<GuildSettingsDto?> GetSettingsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == discordUserId && g.IsActive,
                cancellationToken);

        if (guild is null || guild.Settings is null)
        {
            return null;
        }

        return MapSettings(guild.Id, guild.Settings);
    }

    public async Task<GuildSettingsDto?> UpdateSettingsAsync(
        Guid guildId,
        string discordUserId,
        UpdateGuildSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == discordUserId && g.IsActive,
                cancellationToken);

        if (guild is null)
        {
            return null;
        }

        if (guild.Settings is null)
        {
            guild.Settings = new GuildSettings { GuildId = guild.Id };
            _dbContext.GuildSettings.Add(guild.Settings);
        }

        guild.Settings.WelcomeEnabled = request.WelcomeEnabled;
        guild.Settings.WelcomeChannelId = request.WelcomeChannelId;
        guild.Settings.WelcomeMessage = request.WelcomeMessage;
        guild.Settings.AutoRoleEnabled = request.AutoRoleEnabled;
        guild.Settings.AutoRoleId = request.AutoRoleId;
        guild.Settings.LogsEnabled = request.LogsEnabled;
        guild.Settings.LogChannelId = request.LogChannelId;
        guild.Settings.TicketCategoryId = request.TicketCategoryId;
        guild.Settings.TicketWelcomeTitle = request.TicketWelcomeTitle.Trim();
        guild.Settings.TicketWelcomeMessage = request.TicketWelcomeMessage.Trim();
        guild.Settings.TicketClosedMessage = request.TicketClosedMessage.Trim();
        guild.Settings.TicketClosedFromDashboardMessage = request.TicketClosedFromDashboardMessage.Trim();
        guild.Settings.TicketStaffReplyPrefix = request.TicketStaffReplyPrefix.Trim();

        var normalizedButtons = CommandPanelSerializer.SerializeButtons(request.CommandPanelButtons);
        var panelChanged = CommandPanelService.ShouldRequestRefresh(guild.Settings, request);

        guild.Settings.CommandPanelEnabled = request.CommandPanelEnabled;
        guild.Settings.CommandPanelChannelId = request.CommandPanelChannelId;
        guild.Settings.CommandPanelTitle = request.CommandPanelTitle.Trim();
        guild.Settings.CommandPanelDescription = request.CommandPanelDescription.Trim();
        guild.Settings.CommandPanelButtonsJson = normalizedButtons;

        if (request.CommandPanelEnabled
            && !string.IsNullOrWhiteSpace(request.CommandPanelChannelId))
        {
            guild.Settings.CommandPanelRefreshRequested = true;
        }
        else if (panelChanged)
        {
            guild.Settings.CommandPanelRefreshRequested = true;
        }
        else if (!request.CommandPanelEnabled)
        {
            guild.Settings.CommandPanelRefreshRequested = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = guild.DiscordGuildId,
            Type = LogEventType.SettingsUpdated,
            Message = "Server settings updated from the dashboard.",
            ActorDiscordUserId = discordUserId
        }, cancellationToken);

        return MapSettings(guild.Id, guild.Settings);
    }

    public async Task<RegisterGuildResponse> RegisterGuildAsync(
        RegisterGuildRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(g => g.DiscordGuildId == request.DiscordGuildId, cancellationToken);

        var isNew = guild is null;

        if (guild is null)
        {
            guild = new Guild
            {
                DiscordGuildId = request.DiscordGuildId,
                Name = request.Name,
                OwnerDiscordUserId = request.OwnerDiscordUserId,
                IconUrl = request.IconUrl,
                IsActive = true,
                Settings = new GuildSettings()
            };

            _dbContext.Guilds.Add(guild);
        }
        else
        {
            guild.Name = request.Name;
            guild.OwnerDiscordUserId = request.OwnerDiscordUserId;
            guild.IconUrl = request.IconUrl;
            guild.IsActive = true;

            if (guild.Settings is null)
            {
                guild.Settings = new GuildSettings { GuildId = guild.Id };
                _dbContext.GuildSettings.Add(guild.Settings);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _moduleService.EnsureGuildModulesAsync(guild.Id, cancellationToken);
        await _subscriptionService.EnsureGuildSubscriptionAsync(guild.Id, cancellationToken);

        return new RegisterGuildResponse
        {
            Id = guild.Id,
            DiscordGuildId = guild.DiscordGuildId,
            Name = guild.Name,
            IsNew = isNew
        };
    }

    public async Task<GuildSettingsDto?> GetSettingsByDiscordGuildIdAsync(
        string discordGuildId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(
                g => g.DiscordGuildId == discordGuildId && g.IsActive,
                cancellationToken);

        if (guild?.Settings is null)
        {
            return null;
        }

        return MapSettings(guild.Id, guild.Settings);
    }

    public async Task<GuildOverviewDto?> GetOverviewAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await _guildAccessService.GetAccessAsync(guildId, discordUserId, cancellationToken);
        if (access is null || !access.CanAccessOverview)
        {
            return null;
        }

        var overview = await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.Id == guildId && g.IsActive)
            .Select(g => new GuildOverviewDto
            {
                Name = g.Name,
                IconUrl = g.IconUrl,
                IsActive = g.IsActive,
                ResourcesSyncedAt = g.ResourcesSyncedAt,
                TotalChannels = g.Channels.Count,
                TotalRoles = g.Roles.Count,
                TotalTickets = g.Tickets.Count,
                OpenTickets = g.Tickets.Count(t => t.Status == TicketStatus.Open),
                ClosedTickets = g.Tickets.Count(t => t.Status == TicketStatus.Closed),
                WelcomeEnabled = g.Settings != null && g.Settings.WelcomeEnabled,
                AutoRoleEnabled = g.Settings != null && g.Settings.AutoRoleEnabled,
                LogsEnabled = g.Settings != null && g.Settings.LogsEnabled,
                TicketsEnabled = g.Settings != null && g.Settings.TicketsEnabled
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (overview is null)
        {
            return null;
        }

        var onboarding = await _onboardingService.GetGuildChecklistAsync(guildId, discordUserId, cancellationToken);

        return new GuildOverviewDto
        {
            Name = overview.Name,
            IconUrl = overview.IconUrl,
            IsActive = overview.IsActive,
            ResourcesSyncedAt = overview.ResourcesSyncedAt,
            TotalChannels = overview.TotalChannels,
            TotalRoles = overview.TotalRoles,
            TotalTickets = overview.TotalTickets,
            OpenTickets = overview.OpenTickets,
            ClosedTickets = overview.ClosedTickets,
            WelcomeEnabled = overview.WelcomeEnabled,
            AutoRoleEnabled = overview.AutoRoleEnabled,
            LogsEnabled = overview.LogsEnabled,
            TicketsEnabled = overview.TicketsEnabled,
            Onboarding = onboarding
        };
    }

    private static GuildSettingsDto MapSettings(Guid guildId, GuildSettings settings) =>
        new()
        {
            GuildId = guildId,
            WelcomeEnabled = settings.WelcomeEnabled,
            WelcomeChannelId = settings.WelcomeChannelId,
            WelcomeMessage = settings.WelcomeMessage,
            AutoRoleEnabled = settings.AutoRoleEnabled,
            AutoRoleId = settings.AutoRoleId,
            LogsEnabled = settings.LogsEnabled,
            LogChannelId = settings.LogChannelId,
            TicketsEnabled = settings.TicketsEnabled,
            TicketCategoryId = settings.TicketCategoryId,
            TicketWelcomeTitle = settings.TicketWelcomeTitle,
            TicketWelcomeMessage = settings.TicketWelcomeMessage,
            TicketClosedMessage = settings.TicketClosedMessage,
            TicketClosedFromDashboardMessage = settings.TicketClosedFromDashboardMessage,
            TicketStaffReplyPrefix = settings.TicketStaffReplyPrefix,
            CommandPanelEnabled = settings.CommandPanelEnabled,
            CommandPanelChannelId = settings.CommandPanelChannelId,
            CommandPanelTitle = settings.CommandPanelTitle,
            CommandPanelDescription = settings.CommandPanelDescription,
            CommandPanelButtons = CommandPanelSerializer.ParseButtons(settings.CommandPanelButtonsJson)
        };
}
