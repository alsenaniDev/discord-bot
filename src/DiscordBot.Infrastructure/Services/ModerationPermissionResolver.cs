using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IModerationPermissionResolver
{
    Task<EvaluateModerationPermissionsResponse?> ResolveByDiscordGuildIdAsync(
        string discordGuildId,
        string discordUserId,
        IReadOnlyList<string>? liveDiscordRoleIds = null,
        CancellationToken cancellationToken = default);
}

public class ModerationPermissionResolver : IModerationPermissionResolver
{
    private readonly AppDbContext _dbContext;
    private readonly IPlatformAdminService _platformAdminService;

    public ModerationPermissionResolver(
        AppDbContext dbContext,
        IPlatformAdminService platformAdminService)
    {
        _dbContext = dbContext;
        _platformAdminService = platformAdminService;
    }

    public async Task<EvaluateModerationPermissionsResponse?> ResolveByDiscordGuildIdAsync(
        string discordGuildId,
        string discordUserId,
        IReadOnlyList<string>? liveDiscordRoleIds = null,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        if (guild.OwnerDiscordUserId == discordUserId
            || await _platformAdminService.IsAdminAsync(discordUserId, cancellationToken))
        {
            return FullAccess();
        }

        var userRoleIds = liveDiscordRoleIds?.ToList()
            ?? await LoadSyncedRoleIdsAsync(guild.Id, discordUserId, cancellationToken);

        if (userRoleIds.Count == 0)
        {
            return Empty();
        }

        var permissionRoles = await _dbContext.ModerationPermissionRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guild.Id && userRoleIds.Contains(r.RoleDiscordId))
            .ToListAsync(cancellationToken);

        if (permissionRoles.Count == 0)
        {
            return Empty();
        }

        var canWarn = permissionRoles.Any(r => r.CanWarn);
        var canViewWarnings = permissionRoles.Any(r => r.CanViewWarnings);
        var canClear = permissionRoles.Any(r => r.CanClearMessages);
        var canKick = permissionRoles.Any(r => r.CanKick);
        var canViewCases = permissionRoles.Any(r => r.CanViewModerationCases);
        var canViewLogs = permissionRoles.Any(r => r.CanViewLogs);

        return new EvaluateModerationPermissionsResponse
        {
            CanWarn = canWarn,
            CanViewWarnings = canViewWarnings,
            CanClearMessages = canClear,
            CanKick = canKick,
            CanViewModerationCases = canViewCases,
            CanViewLogs = canViewLogs,
            CanAccessModeration = canWarn || canViewWarnings || canClear || canKick || canViewCases || canViewLogs
        };
    }

    private async Task<List<string>> LoadSyncedRoleIdsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken)
    {
        var member = await _dbContext.DiscordGuildMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.GuildId == guildId && m.DiscordUserId == discordUserId,
                cancellationToken);

        return member is null ? [] : GuildPermissionResolver.ParseRoleIds(member.DiscordRoleIdsJson);
    }

    private static EvaluateModerationPermissionsResponse FullAccess() =>
        new()
        {
            CanWarn = true,
            CanViewWarnings = true,
            CanClearMessages = true,
            CanKick = true,
            CanViewModerationCases = true,
            CanViewLogs = true,
            CanAccessModeration = true
        };

    private static EvaluateModerationPermissionsResponse Empty() =>
        new()
        {
            CanWarn = false,
            CanViewWarnings = false,
            CanClearMessages = false,
            CanKick = false,
            CanViewModerationCases = false,
            CanViewLogs = false,
            CanAccessModeration = false
        };
}
