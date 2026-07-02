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

    Task<bool> CanViewTicketsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<bool> CanReplyToTicketsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<bool> CanCloseTicketsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class GuildAccessService : IGuildAccessService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildPermissionResolver _permissionResolver;
    private readonly IPlatformAdminService _platformAdminService;

    public GuildAccessService(
        AppDbContext dbContext,
        IGuildPermissionResolver permissionResolver,
        IPlatformAdminService platformAdminService)
    {
        _dbContext = dbContext;
        _permissionResolver = permissionResolver;
        _platformAdminService = platformAdminService;
    }

    public Task<bool> IsPlatformAdminAsync(string discordUserId, CancellationToken cancellationToken = default) =>
        _platformAdminService.IsAdminAsync(discordUserId, cancellationToken);

    public async Task<GuildAccessDto?> GetAccessAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await _permissionResolver.ResolveAsync(guildId, discordUserId, cancellationToken: cancellationToken);
        return resolved is null ? null : GuildPermissionMapper.ToAccessDto(resolved);
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

    public async Task<bool> CanViewTicketsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await _permissionResolver.ResolveAsync(guildId, discordUserId, cancellationToken: cancellationToken);
        return resolved is not null && GuildPermissionMapper.CanViewTickets(resolved);
    }

    public async Task<bool> CanReplyToTicketsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await _permissionResolver.ResolveAsync(guildId, discordUserId, cancellationToken: cancellationToken);
        return resolved is not null && GuildPermissionMapper.CanReplyToTickets(resolved);
    }

    public async Task<bool> CanCloseTicketsAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await _permissionResolver.ResolveAsync(guildId, discordUserId, cancellationToken: cancellationToken);
        return resolved is not null && GuildPermissionMapper.CanCloseTickets(resolved);
    }
}
