using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildAccessService
{
    Task<bool> IsPlatformAdminAsync(string discordUserId, CancellationToken cancellationToken = default);

    Task<GuildAccessDto?> GetAccessAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<bool> CanAccessModerationPagesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageStaffAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<bool> IsOwnerAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class GuildAccessService : IGuildAccessService
{
    private readonly AppDbContext _dbContext;
    private readonly IPlatformAdminService _platformAdminService;

    public GuildAccessService(AppDbContext dbContext, IPlatformAdminService platformAdminService)
    {
        _dbContext = dbContext;
        _platformAdminService = platformAdminService;
    }

    public Task<bool> IsPlatformAdminAsync(string discordUserId, CancellationToken cancellationToken = default) =>
        _platformAdminService.IsAdminAsync(discordUserId, cancellationToken);

    public async Task<GuildAccessDto?> GetAccessAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var isPlatformAdmin = await IsPlatformAdminAsync(discordUserId, cancellationToken);
        var isOwner = guild.OwnerDiscordUserId == discordUserId;

        GuildStaffRole? staffRole = null;
        if (!isOwner && !isPlatformAdmin)
        {
            var staff = await _dbContext.GuildStaff
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    s => s.GuildId == guildId && s.DiscordUserId == discordUserId,
                    cancellationToken);

            if (staff is null)
            {
                return null;
            }

            staffRole = staff.Role;
        }

        var isStaff = staffRole.HasValue;
        var canModerate = isOwner || isPlatformAdmin || isStaff;
        var canManage = isOwner || isPlatformAdmin;

        return new GuildAccessDto
        {
            IsOwner = isOwner,
            IsPlatformAdmin = isPlatformAdmin,
            StaffRole = staffRole?.ToString(),
            CanManageSettings = canManage,
            CanManageModules = canManage,
            CanManageSubscription = canManage,
            CanManageStaff = canManage,
            CanAccessModeration = canModerate,
            CanAccessLogs = canModerate,
            CanAccessTickets = canModerate,
            CanAccessOverview = canManage
        };
    }

    public async Task<bool> CanAccessModerationPagesAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(guildId, discordUserId, cancellationToken);
        return access?.CanAccessModeration == true;
    }

    public async Task<bool> CanManageStaffAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(guildId, discordUserId, cancellationToken);
        return access?.CanManageStaff == true;
    }

    public async Task<bool> IsOwnerAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Guilds
            .AsNoTracking()
            .AnyAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == discordUserId && g.IsActive,
                cancellationToken);
    }
}
