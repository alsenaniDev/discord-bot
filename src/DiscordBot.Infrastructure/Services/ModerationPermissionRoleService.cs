using DiscordBot.Domain.Entities;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IModerationPermissionRoleService
{
    Task<IReadOnlyList<ModerationPermissionRoleDto>> GetRolesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<ModerationPermissionRoleDto?> CreateAsync(
        Guid guildId,
        string discordUserId,
        CreateModerationPermissionRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<ModerationPermissionRoleDto?> UpdateAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        UpdateModerationPermissionRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class ModerationPermissionRoleService : IModerationPermissionRoleService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;

    public ModerationPermissionRoleService(
        AppDbContext dbContext,
        IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
    }

    public async Task<IReadOnlyList<ModerationPermissionRoleDto>> GetRolesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageAsync(guildId, discordUserId, cancellationToken))
        {
            return [];
        }

        var roles = await _dbContext.ModerationPermissionRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderBy(r => r.RoleDiscordId)
            .ToListAsync(cancellationToken);

        var discordRoles = await _dbContext.DiscordRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .ToDictionaryAsync(r => r.DiscordRoleId, r => r.Name, cancellationToken);

        return roles
            .Select(role => Map(role, discordRoles.GetValueOrDefault(role.RoleDiscordId)))
            .ToList();
    }

    public async Task<ModerationPermissionRoleDto?> CreateAsync(
        Guid guildId,
        string discordUserId,
        CreateModerationPermissionRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var roleDiscordId = request.RoleDiscordId.Trim();
        var exists = await _dbContext.ModerationPermissionRoles.AnyAsync(
            r => r.GuildId == guildId && r.RoleDiscordId == roleDiscordId,
            cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("This Discord role already has moderation permissions configured.");
        }

        var entity = new ModerationPermissionRole
        {
            GuildId = guildId,
            RoleDiscordId = roleDiscordId,
            CanWarn = request.CanWarn,
            CanViewWarnings = request.CanViewWarnings,
            CanClearMessages = request.CanClearMessages,
            CanKick = request.CanKick,
            CanViewModerationCases = request.CanViewModerationCases,
            CanViewLogs = request.CanViewLogs
        };

        _dbContext.ModerationPermissionRoles.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var discordRoleName = await _dbContext.DiscordRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId && r.DiscordRoleId == roleDiscordId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return Map(entity, discordRoleName);
    }

    public async Task<ModerationPermissionRoleDto?> UpdateAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        UpdateModerationPermissionRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var entity = await _dbContext.ModerationPermissionRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.GuildId == guildId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var roleDiscordId = request.RoleDiscordId.Trim();
        var duplicate = await _dbContext.ModerationPermissionRoles.AnyAsync(
            r => r.GuildId == guildId && r.RoleDiscordId == roleDiscordId && r.Id != roleId,
            cancellationToken);

        if (duplicate)
        {
            throw new InvalidOperationException("Another entry already uses this Discord role.");
        }

        entity.RoleDiscordId = roleDiscordId;
        entity.CanWarn = request.CanWarn;
        entity.CanViewWarnings = request.CanViewWarnings;
        entity.CanClearMessages = request.CanClearMessages;
        entity.CanKick = request.CanKick;
        entity.CanViewModerationCases = request.CanViewModerationCases;
        entity.CanViewLogs = request.CanViewLogs;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var discordRoleName = await _dbContext.DiscordRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId && r.DiscordRoleId == roleDiscordId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return Map(entity, discordRoleName);
    }

    public async Task<bool> DeleteAsync(
        Guid guildId,
        Guid roleId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await CanManageAsync(guildId, discordUserId, cancellationToken))
        {
            return false;
        }

        var entity = await _dbContext.ModerationPermissionRoles
            .FirstOrDefaultAsync(r => r.Id == roleId && r.GuildId == guildId, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        _dbContext.ModerationPermissionRoles.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> CanManageAsync(Guid guildId, string discordUserId, CancellationToken cancellationToken)
    {
        var access = await _guildAccessService.GetAccessAsync(guildId, discordUserId, cancellationToken);
        return access?.CanManageSettings == true;
    }

    private static ModerationPermissionRoleDto Map(ModerationPermissionRole role, string? discordRoleName) =>
        new()
        {
            Id = role.Id,
            GuildId = role.GuildId,
            RoleDiscordId = role.RoleDiscordId,
            RoleName = discordRoleName,
            CanWarn = role.CanWarn,
            CanViewWarnings = role.CanViewWarnings,
            CanClearMessages = role.CanClearMessages,
            CanKick = role.CanKick,
            CanViewModerationCases = role.CanViewModerationCases,
            CanViewLogs = role.CanViewLogs,
            CreatedAt = role.CreatedAt,
            UpdatedAt = role.UpdatedAt
        };
}
