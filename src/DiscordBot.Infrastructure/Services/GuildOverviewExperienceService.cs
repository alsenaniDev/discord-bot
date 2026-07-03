using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildOverviewExperienceService
{
    Task<GuildOverviewExperienceDto> BuildAsync(
        Guid guildId,
        GuildOverviewDto overview,
        OnboardingChecklistDto? checklist,
        CancellationToken cancellationToken = default);
}

public class GuildOverviewExperienceService : IGuildOverviewExperienceService
{
    private readonly AppDbContext _dbContext;

    public GuildOverviewExperienceService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<GuildOverviewExperienceDto> BuildAsync(
        Guid guildId,
        GuildOverviewDto overview,
        OnboardingChecklistDto? checklist,
        CancellationToken cancellationToken = default)
    {
        var checklistState = checklist ?? new OnboardingChecklistDto();

        var guildData = await _dbContext.Guilds
            .AsNoTracking()
            .Include(g => g.Settings)
            .Include(g => g.Subscription!)
                .ThenInclude(s => s.SubscriptionPlan)
            .FirstAsync(g => g.Id == guildId, cancellationToken);

        var settings = guildData.Settings;
        var subscription = guildData.Subscription;

        var permissionRoleCount = await _dbContext.GuildPermissionRoles
            .CountAsync(r => r.GuildId == guildId, cancellationToken);

        var reactionRoleCount = await _dbContext.ReactionRoles
            .CountAsync(r => r.GuildId == guildId && r.IsActive, cancellationToken);

        var recentLogCount = await _dbContext.LogEntries
            .CountAsync(
                l => l.GuildId == guildId && l.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-7),
                cancellationToken);

        var logsConfigured = settings?.LogsEnabled == true
            && !string.IsNullOrWhiteSpace(settings.LogChannelId);

        var firstValueAchieved = overview.TotalTickets > 0
            || (checklistState.WelcomeConfigured && overview.WelcomeEnabled)
            || logsConfigured && recentLogCount > 0
            || reactionRoleCount > 0;

        var moduleConfigured = checklistState.WelcomeConfigured
            || checklistState.TicketsConfigured
            || logsConfigured;

        var activation = BuildActivationProgress(
            checklistState,
            moduleConfigured,
            firstValueAchieved,
            permissionRoleCount > 0,
            logsConfigured);

        var subscriptionSummary = BuildSubscriptionSummary(subscription);
        var botOnline = overview.IsActive
            && checklistState.ResourcesSynced
            && overview.ResourcesSyncedAt is not null
            && overview.ResourcesSyncedAt > DateTimeOffset.UtcNow.AddDays(-7);

        var health = BuildHealthScore(
            overview,
            checklistState,
            activation.IsActivated,
            checklistState.TicketsConfigured,
            logsConfigured,
            permissionRoleCount > 0,
            subscriptionSummary,
            botOnline,
            recentLogCount,
            overview.TotalTickets > 0);

        var recommendations = BuildRecommendations(
            checklistState,
            overview,
            activation,
            logsConfigured,
            permissionRoleCount,
            reactionRoleCount,
            subscriptionSummary);

        var recentActivity = await BuildRecentActivityAsync(guildId, cancellationToken);

        return new GuildOverviewExperienceDto
        {
            Subscription = subscriptionSummary,
            BotOnline = botOnline,
            Activation = activation,
            Health = health,
            Recommendations = recommendations,
            RecentActivity = recentActivity
        };
    }

    private static OverviewSubscriptionSummaryDto BuildSubscriptionSummary(
        Domain.Entities.GuildSubscription? subscription)
    {
        if (subscription?.SubscriptionPlan is null)
        {
            return new OverviewSubscriptionSummaryDto();
        }

        var isExpired = subscription.Status == GuildSubscriptionStatus.Expired
            || (subscription.ExpiresAt is not null && subscription.ExpiresAt <= DateTimeOffset.UtcNow);

        return new OverviewSubscriptionSummaryDto
        {
            PlanKey = subscription.SubscriptionPlan.Key,
            PlanName = subscription.SubscriptionPlan.Name,
            Status = subscription.Status.ToString(),
            ExpiresAt = subscription.ExpiresAt,
            IsPaid = subscription.SubscriptionPlan.Key != PlanKeys.Free,
            IsExpired = isExpired
        };
    }

    private static ActivationProgressDto BuildActivationProgress(
        OnboardingChecklistDto checklist,
        bool moduleConfigured,
        bool firstValueAchieved,
        bool staffConfigured,
        bool logsConfigured)
    {
        var steps = new List<ActivationStepDto>
        {
            new()
            {
                Key = "addBot",
                Phase = "A",
                Completed = checklist.BotInvited,
                Weight = 15,
                ActionRoute = "/servers"
            },
            new()
            {
                Key = "linkGuild",
                Phase = "A",
                Completed = checklist.ResourcesSynced,
                Weight = 20,
                ActionRoute = "/servers"
            },
            new()
            {
                Key = "enableModule",
                Phase = "B",
                Completed = checklist.ModulesEnabled,
                Weight = 15,
                ActionRoute = "modules"
            },
            new()
            {
                Key = "configureModule",
                Phase = "B",
                Completed = moduleConfigured,
                Weight = 20,
                ActionRoute = "settings"
            },
            new()
            {
                Key = "firstValue",
                Phase = "B",
                Completed = firstValueAchieved,
                Weight = 20,
                ActionRoute = "tickets"
            },
            new()
            {
                Key = "inviteStaff",
                Phase = "C",
                Completed = staffConfigured,
                Weight = 5,
                ActionRoute = "staff"
            },
            new()
            {
                Key = "enableLogs",
                Phase = "C",
                Completed = logsConfigured,
                Weight = 5,
                ActionRoute = "settings"
            },
            new()
            {
                Key = "reviewSubscription",
                Phase = "C",
                Completed = checklist.PlanSelected,
                Weight = 5,
                ActionRoute = "subscription"
            }
        };

        var earned = steps.Where(s => s.Completed).Sum(s => s.Weight);
        var progressPercent = earned;
        var isActivated = progressPercent >= 85;

        var current = steps.FirstOrDefault(s => !s.Completed);
        var primaryCtaKey = current?.Key ?? "exploreModules";
        var primaryRoute = current?.ActionRoute ?? "modules";

        return new ActivationProgressDto
        {
            ProgressPercent = progressPercent,
            IsActivated = isActivated,
            CurrentStepKey = current?.Key,
            PrimaryCtaKey = primaryCtaKey,
            PrimaryActionRoute = primaryRoute,
            Steps = steps
        };
    }

    private static CommunityHealthDto BuildHealthScore(
        GuildOverviewDto overview,
        OnboardingChecklistDto checklist,
        bool activationComplete,
        bool ticketsConfigured,
        bool logsConfigured,
        bool permissionsConfigured,
        OverviewSubscriptionSummaryDto subscription,
        bool botOnline,
        int recentLogCount,
        bool hasTickets)
    {
        var factors = new List<HealthFactorDto>
        {
            Factor("guildLinked", overview.IsActive, 15),
            Factor("botOnline", botOnline, 10),
            Factor("modulesEnabled", checklist.ModulesEnabled, 10),
            Factor("activationComplete", activationComplete, 20),
            Factor("ticketsConfigured", ticketsConfigured, 10),
            Factor("logsConfigured", logsConfigured, 10),
            Factor("permissionsConfigured", permissionsConfigured, 10),
            Factor(
                "subscriptionActive",
                subscription.IsPaid && !subscription.IsExpired,
                5),
            Factor(
                "recentActivity",
                recentLogCount > 0 || hasTickets,
                10)
        };

        var score = factors.Sum(f => f.PointsEarned);
        var level = score switch
        {
            >= 90 => "Excellent",
            >= 70 => "Good",
            >= 40 => "NeedsAttention",
            _ => "Critical"
        };

        return new CommunityHealthDto
        {
            Score = score,
            Level = level,
            Factors = factors
        };
    }

    private static HealthFactorDto Factor(string key, bool passed, int points)
    {
        return new HealthFactorDto
        {
            Key = key,
            Passed = passed,
            PointsEarned = passed ? points : 0,
            PointsPossible = points,
            IsWarning = !passed && points >= 10
        };
    }

    private static IReadOnlyList<OverviewRecommendationDto> BuildRecommendations(
        OnboardingChecklistDto checklist,
        GuildOverviewDto overview,
        ActivationProgressDto activation,
        bool logsConfigured,
        int permissionRoleCount,
        int reactionRoleCount,
        OverviewSubscriptionSummaryDto subscription)
    {
        var candidates = new List<(OverviewRecommendationDto Item, int Score)>();

        void Add(string id, string priority, string route, int score)
        {
            candidates.Add((
                new OverviewRecommendationDto
                {
                    Id = id,
                    Priority = priority,
                    Route = route,
                    SortOrder = 0
                },
                score));
        }

        if (!checklist.ResourcesSynced)
        {
            Add("syncResources", "High", "/servers", 300);
        }

        if (!checklist.ModulesEnabled)
        {
            Add("enableModules", "High", "modules", 280);
        }

        if (!checklist.WelcomeConfigured && overview.WelcomeEnabled)
        {
            Add("configureWelcome", "High", "settings", 260);
        }
        else if (!checklist.WelcomeConfigured)
        {
            Add("configureWelcome", "Medium", "settings", 200);
        }

        if (overview.TicketsEnabled && !checklist.TicketsConfigured)
        {
            Add("createTicketPanel", "High", "settings", 250);
        }

        if (overview.TicketsEnabled && checklist.TicketsConfigured && overview.TotalTickets == 0)
        {
            Add("openFirstTicket", "High", "tickets", 240);
        }

        if (!logsConfigured)
        {
            Add("enableLogs", "Medium", "settings", 180);
        }

        if (permissionRoleCount == 0 && activation.IsActivated)
        {
            Add("inviteStaff", "High", "staff", 220);
        }

        if (overview.TicketsEnabled && reactionRoleCount == 0)
        {
            Add("createReactionPanel", "Medium", "reaction-roles", 150);
        }

        if (subscription.IsPaid && subscription.ExpiresAt is not null)
        {
            var daysUntilExpiry = (subscription.ExpiresAt.Value - DateTimeOffset.UtcNow).TotalDays;
            if (daysUntilExpiry <= 7 && daysUntilExpiry > 0)
            {
                Add("renewSubscription", "High", "subscription", 270);
            }
        }
        else if (!subscription.IsPaid && activation.IsActivated)
        {
            Add("upgradeSubscription", "Low", "subscription", 100);
        }

        return candidates
            .OrderByDescending(c => c.Score)
            .Take(3)
            .Select((c, index) => new OverviewRecommendationDto
            {
                Id = c.Item.Id,
                Priority = c.Item.Priority,
                Route = c.Item.Route,
                SortOrder = index + 1
            })
            .ToList();
    }

    private async Task<IReadOnlyList<OverviewActivityItemDto>> BuildRecentActivityAsync(
        Guid guildId,
        CancellationToken cancellationToken)
    {
        var logItems = await _dbContext.LogEntries
            .AsNoTracking()
            .Where(l => l.GuildId == guildId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(5)
            .Select(l => new OverviewActivityItemDto
            {
                Type = "LogEntry",
                Message = l.Message,
                OccurredAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var ticketItems = await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.GuildId == guildId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(3)
            .Select(t => new OverviewActivityItemDto
            {
                Type = "TicketCreated",
                Message = $"Ticket #{t.TicketNumber} opened",
                OccurredAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var moduleItems = await _dbContext.GuildModules
            .AsNoTracking()
            .Include(gm => gm.Module)
            .Where(gm => gm.GuildId == guildId && gm.IsEnabled)
            .OrderByDescending(gm => gm.UpdatedAt)
            .Take(2)
            .Select(gm => new OverviewActivityItemDto
            {
                Type = "ModuleEnabled",
                Message = $"{gm.Module.Name} module enabled",
                OccurredAt = gm.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return logItems
            .Concat(ticketItems)
            .Concat(moduleItems)
            .OrderByDescending(a => a.OccurredAt)
            .Take(8)
            .ToList();
    }
}
