using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildPermissionRoleService
{
    Task<IReadOnlyList<GuildPermissionRoleDto>> GetRolesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<GuildPermissionRoleDto?> CreateAsync(
        Guid guildId,
        string discordUserId,
        CreateGuildPermissionRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<GuildPermissionRoleDto?> UpdateAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        UpdateGuildPermissionRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class GuildPermissionRoleService : IGuildPermissionRoleService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;

    public GuildPermissionRoleService(AppDbContext dbContext, IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
    }

    public async Task<IReadOnlyList<GuildPermissionRoleDto>> GetRolesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanManageStaffAsync(guildId, discordUserId, cancellationToken))
        {
            return [];
        }

        var roles = await _dbContext.GuildPermissionRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var discordRoles = await _dbContext.DiscordRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .ToDictionaryAsync(r => r.DiscordRoleId, r => r.Name, cancellationToken);

        return roles
            .Select(role => Map(role, discordRoles.GetValueOrDefault(role.DiscordRoleId)))
            .ToList();
    }

    public async Task<GuildPermissionRoleDto?> CreateAsync(
        Guid guildId,
        string discordUserId,
        CreateGuildPermissionRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanManageStaffAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.DiscordRoleId))
        {
            return null;
        }

        var exists = await _dbContext.GuildPermissionRoles
            .AnyAsync(
                r => r.GuildId == guildId && r.DiscordRoleId == request.DiscordRoleId.Trim(),
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("This Discord role is already mapped to a permission role.");
        }

        var role = new GuildPermissionRole
        {
            GuildId = guildId,
            Name = request.Name.Trim(),
            DiscordRoleId = request.DiscordRoleId.Trim(),
            Permissions = ParsePermissions(request.PermissionKeys)
        };

        _dbContext.GuildPermissionRoles.Add(role);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var discordRoleName = await _dbContext.DiscordRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId && r.DiscordRoleId == role.DiscordRoleId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return Map(role, discordRoleName);
    }

    public async Task<GuildPermissionRoleDto?> UpdateAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        UpdateGuildPermissionRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanManageStaffAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var role = await _dbContext.GuildPermissionRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.GuildId == guildId, cancellationToken);

        if (role is null)
        {
            return null;
        }

        var duplicate = await _dbContext.GuildPermissionRoles
            .AnyAsync(
                r => r.GuildId == guildId
                     && r.DiscordRoleId == request.DiscordRoleId.Trim()
                     && r.Id != roleId,
                cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("This Discord role is already mapped to a permission role.");
        }

        role.Name = request.Name.Trim();
        role.DiscordRoleId = request.DiscordRoleId.Trim();
        role.Permissions = ParsePermissions(request.PermissionKeys);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var discordRoleName = await _dbContext.DiscordRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId && r.DiscordRoleId == role.DiscordRoleId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return Map(role, discordRoleName);
    }

    public async Task<bool> DeleteAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanManageStaffAsync(guildId, discordUserId, cancellationToken))
        {
            return false;
        }

        var role = await _dbContext.GuildPermissionRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.GuildId == guildId, cancellationToken);

        if (role is null)
        {
            return false;
        }

        _dbContext.GuildPermissionRoles.Remove(role);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static GuildPermissionRoleDto Map(GuildPermissionRole role, string? discordRoleName) =>
        new()
        {
            Id = role.Id,
            GuildId = role.GuildId,
            Name = role.Name,
            DiscordRoleId = role.DiscordRoleId,
            DiscordRoleName = discordRoleName,
            Permissions = role.Permissions,
            PermissionKeys = ToPermissionKeys(role.Permissions),
            CreatedAt = role.CreatedAt
        };

    internal static IReadOnlyList<string> ToPermissionKeys(GuildPermissions permissions)
    {
        var keys = new List<string>();
        foreach (GuildPermissions value in Enum.GetValues<GuildPermissions>())
        {
            if (value == GuildPermissions.None)
            {
                continue;
            }

            if (permissions.HasFlag(value))
            {
                keys.Add(value.ToString());
            }
        }

        return keys;
    }

    internal static GuildPermissions ParsePermissions(IEnumerable<string> permissionKeys)
    {
        var permissions = GuildPermissions.None;

        foreach (var key in permissionKeys)
        {
            if (Enum.TryParse<GuildPermissions>(key, ignoreCase: true, out var flag)
                && flag != GuildPermissions.None)
            {
                permissions |= flag;
            }
        }

        return permissions;
    }
}
