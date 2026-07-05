using System.Net.Http.Headers;
using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using DiscordBot.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace DiscordBot.Infrastructure.Services;

public interface IOnboardingService
{
    Task<OnboardingStatusDto> GetStatusAsync(string discordUserId, CancellationToken cancellationToken = default);

    Task<OnboardingChecklistDto?> GetGuildChecklistAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordGuildOnboardingDto>> GetMyDiscordGuildsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public class OnboardingService : IOnboardingService
{
    private const long DefaultBotPermissions = 268513278;
    private const ulong Administrator = 0x0000000000000008;
    private const ulong ManageGuild = 0x0000000000000020;

    private readonly AppDbContext _dbContext;
    private readonly DiscordOptions _discordOptions;
    private readonly HttpClient _httpClient;

    public OnboardingService(AppDbContext dbContext, IOptions<DiscordOptions> discordOptions, HttpClient httpClient)
    {
        _dbContext = dbContext;
        _discordOptions = discordOptions.Value;
        _httpClient = httpClient;
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

    private string BuildBotInviteUrlForGuild(string discordGuildId)
    {
        if (string.IsNullOrWhiteSpace(_discordOptions.ClientId))
        {
            return string.Empty;
        }

        return
            $"https://discord.com/oauth2/authorize?client_id={Uri.EscapeDataString(_discordOptions.ClientId.Trim())}" +
            $"&permissions={DefaultBotPermissions}" +
            $"&scope=bot%20applications.commands" +
            $"&guild_id={Uri.EscapeDataString(discordGuildId)}" +
            $"&disable_guild_select=true";
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

    public async Task<IReadOnlyList<DiscordGuildOnboardingDto>> GetMyDiscordGuildsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("User not found.");
        }

        if (string.IsNullOrWhiteSpace(user.DiscordAccessToken))
        {
            throw new InvalidOperationException("Discord account must be reconnected with the guilds scope.");
        }

        var discordGuilds = await RequestDiscordGuildsAsync(user.DiscordAccessToken, cancellationToken);

        var manageableGuilds = discordGuilds
            .Where(CanManageGuild)
            .ToList();

        var discordGuildIds = manageableGuilds
            .Select(g => g.Id)
            .ToList();

        var installedGuilds = await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => discordGuildIds.Contains(g.DiscordGuildId) && g.IsActive)
            .Select(g => new
            {
                g.Id,
                g.DiscordGuildId
            })
            .ToDictionaryAsync(g => g.DiscordGuildId, g => g.Id, cancellationToken);

        return manageableGuilds
            .Select(g =>
            {
                var botInstalled = installedGuilds.TryGetValue(g.Id, out var platformGuildId);

                return new DiscordGuildOnboardingDto
                {
                    DiscordGuildId = g.Id,
                    Name = g.Name,
                    IconUrl = BuildGuildIconUrl(g.Id, g.Icon),
                    IsOwner = g.Owner,
                    CanManage = true,
                    BotInstalled = botInstalled,
                    PlatformGuildId = botInstalled ? platformGuildId : null,
                    Action = botInstalled ? "manage" : "add_bot",
                    InviteUrl = botInstalled ? null : BuildBotInviteUrlForGuild(g.Id)
                };
            })
            .OrderBy(g => g.Name)
            .ToList();
    }

    private async Task<IReadOnlyList<DiscordUserGuildResponse>> RequestDiscordGuildsAsync(string accessToken, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "https://discord.com/api/users/@me/guilds");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Discord token expired. Please sign in again.");
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<DiscordUserGuildResponse>>(
            cancellationToken: cancellationToken) ?? [];
    }

    private static bool CanManageGuild(DiscordUserGuildResponse guild)
    {
        if (guild.Owner)
        {
            return true;
        }

        if (!TryReadPermissions(guild.Permissions, out var permissions))
        {
            return false;
        }

        return (permissions & Administrator) == Administrator
            || (permissions & ManageGuild) == ManageGuild;
    }

    private static bool TryReadPermissions(JsonElement value, out ulong permissions)
    {
        permissions = 0;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetUInt64(out permissions),
            JsonValueKind.String => ulong.TryParse(value.GetString(), out permissions),
            _ => false
        };
    }

    private static string? BuildGuildIconUrl(string guildId, string? iconHash)
    {
        if (string.IsNullOrWhiteSpace(iconHash))
        {
            return null;
        }

        return $"https://cdn.discordapp.com/icons/{guildId}/{iconHash}.png";
    }
}
