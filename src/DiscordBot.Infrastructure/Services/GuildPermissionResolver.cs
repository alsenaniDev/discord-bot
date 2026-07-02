using System.Text.Json;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildPermissionResolver
{
    Task<ResolvedGuildPermissions?> ResolveAsync(
        Guid guildId,
        string discordUserId,
        IReadOnlyList<string>? liveDiscordRoleIds = null,
        CancellationToken cancellationToken = default);

    Task<ResolvedGuildPermissions?> ResolveByDiscordGuildIdAsync(
        string discordGuildId,
        string discordUserId,
        IReadOnlyList<string>? liveDiscordRoleIds = null,
        CancellationToken cancellationToken = default);
}

public sealed class ResolvedGuildPermissions
{
    public required GuildPermissions Permissions { get; init; }
    public required IReadOnlyList<string> MatchedRoleNames { get; init; }
    public bool IsOwner { get; init; }
    public bool IsPlatformAdmin { get; init; }
}

public class GuildPermissionResolver : IGuildPermissionResolver
{
    private readonly AppDbContext _dbContext;
    private readonly IPlatformAdminService _platformAdminService;

    public GuildPermissionResolver(AppDbContext dbContext, IPlatformAdminService platformAdminService)
    {
        _dbContext = dbContext;
        _platformAdminService = platformAdminService;
    }

    public async Task<ResolvedGuildPermissions?> ResolveAsync(
        Guid guildId,
        string discordUserId,
        IReadOnlyList<string>? liveDiscordRoleIds = null,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var isOwner = guild.OwnerDiscordUserId == discordUserId;
        var isPlatformAdmin = await _platformAdminService.IsAdminAsync(discordUserId, cancellationToken);

        if (isOwner || isPlatformAdmin)
        {
            return new ResolvedGuildPermissions
            {
                Permissions = GuildPermissionDefaults.OwnerPermissions,
                MatchedRoleNames = [],
                IsOwner = isOwner,
                IsPlatformAdmin = isPlatformAdmin
            };
        }

        var userRoleIds = liveDiscordRoleIds?.ToList()
            ?? await LoadSyncedRoleIdsAsync(guildId, discordUserId, cancellationToken);

        if (userRoleIds.Count == 0)
        {
            return null;
        }

        var permissionRoles = await _dbContext.GuildPermissionRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId && userRoleIds.Contains(r.DiscordRoleId))
            .ToListAsync(cancellationToken);

        if (permissionRoles.Count == 0)
        {
            return null;
        }

        var permissions = GuildPermissions.None;
        var matchedNames = new List<string>();

        foreach (var role in permissionRoles)
        {
            permissions |= role.Permissions;
            matchedNames.Add(role.Name);
        }

        if (permissions == GuildPermissions.None)
        {
            return null;
        }

        return new ResolvedGuildPermissions
        {
            Permissions = permissions,
            MatchedRoleNames = matchedNames,
            IsOwner = false,
            IsPlatformAdmin = false
        };
    }

    public async Task<ResolvedGuildPermissions?> ResolveByDiscordGuildIdAsync(
        string discordGuildId,
        string discordUserId,
        IReadOnlyList<string>? liveDiscordRoleIds = null,
        CancellationToken cancellationToken = default)
    {
        var guildId = await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.DiscordGuildId == discordGuildId && g.IsActive)
            .Select(g => g.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (guildId == Guid.Empty)
        {
            return null;
        }

        return await ResolveAsync(guildId, discordUserId, liveDiscordRoleIds, cancellationToken);
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

        return member is null ? [] : ParseRoleIds(member.DiscordRoleIdsJson);
    }

    internal static List<string> ParseRoleIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}

public static class GuildPermissionMapper
{
    public static GuildAccessDto ToAccessDto(ResolvedGuildPermissions resolved)
    {
        var permissions = resolved.Permissions;
        var canManage = resolved.IsOwner || resolved.IsPlatformAdmin;
        var canModeratePages = canManage || HasModerationPageAccess(permissions);
        var canUseBotModeration = canManage || HasModerationCommandAccess(permissions);

        return new GuildAccessDto
        {
            IsOwner = resolved.IsOwner,
            IsPlatformAdmin = resolved.IsPlatformAdmin,
            StaffRole = resolved.MatchedRoleNames.Count == 0
                ? null
                : string.Join(", ", resolved.MatchedRoleNames),
            Permissions = permissions,
            CanWarn = canManage,
            CanKick = canManage,
            CanTimeout = canManage,
            CanClearMessages = canManage,
            CanManageSettings = canManage,
            CanManageModules = canManage,
            CanManageSubscription = canManage,
            CanManageStaff = canManage,
            CanAccessModeration = canModeratePages || canUseBotModeration,
            CanAccessLogs = canManage
                || permissions.HasFlag(GuildPermissions.ViewLogs)
                || permissions.HasFlag(GuildPermissions.ClearLogs)
                || canModeratePages,
            CanAccessTickets = canManage || HasTicketAccess(permissions) || canModeratePages,
            CanAccessOverview = canManage,
            CanClearLogs = canManage || permissions.HasFlag(GuildPermissions.ClearLogs)
        };
    }

    public static EvaluatePermissionsResponse ToEvaluatePermissionsResponse(ResolvedGuildPermissions resolved)
    {
        var permissions = resolved.Permissions;
        var canManage = resolved.IsOwner || resolved.IsPlatformAdmin;
        var canUseBotModeration = canManage || HasModerationCommandAccess(permissions);

        return new EvaluatePermissionsResponse
        {
            Permissions = permissions,
            CanWarn = canManage || permissions.HasFlag(GuildPermissions.UseWarn),
            CanKick = canManage || permissions.HasFlag(GuildPermissions.UseKick),
            CanTimeout = canManage || permissions.HasFlag(GuildPermissions.UseTimeout),
            CanClearMessages = canManage || permissions.HasFlag(GuildPermissions.UseClearMessages),
            CanViewWarnings = canManage
                || permissions.HasFlag(GuildPermissions.ViewWarnings)
                || permissions.HasFlag(GuildPermissions.ManageModeration),
            CanViewModerationCases = canManage
                || permissions.HasFlag(GuildPermissions.ViewModerationCases)
                || permissions.HasFlag(GuildPermissions.ManageModeration),
            CanViewLogs = canManage || permissions.HasFlag(GuildPermissions.ViewLogs),
            CanAccessModeration = canUseBotModeration
        };
    }

    public static bool HasModerationPageAccess(GuildPermissions permissions) =>
        permissions.HasFlag(GuildPermissions.ManageModeration)
        || permissions.HasFlag(GuildPermissions.ViewLogs)
        || permissions.HasFlag(GuildPermissions.ViewTickets)
        || permissions.HasFlag(GuildPermissions.ReplyToTickets)
        || permissions.HasFlag(GuildPermissions.CloseTickets)
        || permissions.HasFlag(GuildPermissions.ManageTickets);

    public static bool HasModerationCommandAccess(GuildPermissions permissions) =>
        permissions.HasFlag(GuildPermissions.UseWarn)
        || permissions.HasFlag(GuildPermissions.UseKick)
        || permissions.HasFlag(GuildPermissions.UseTimeout)
        || permissions.HasFlag(GuildPermissions.UseBan)
        || permissions.HasFlag(GuildPermissions.UseClearMessages)
        || permissions.HasFlag(GuildPermissions.ViewWarnings)
        || permissions.HasFlag(GuildPermissions.ViewModerationCases)
        || permissions.HasFlag(GuildPermissions.ViewLogs);

    public static bool HasTicketAccess(GuildPermissions permissions) =>
        permissions.HasFlag(GuildPermissions.ViewTickets)
        || permissions.HasFlag(GuildPermissions.ReplyToTickets)
        || permissions.HasFlag(GuildPermissions.CloseTickets)
        || permissions.HasFlag(GuildPermissions.ManageTickets);
}
