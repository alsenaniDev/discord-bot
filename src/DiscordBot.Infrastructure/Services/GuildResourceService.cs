using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildResourceService
{
    Task<IReadOnlyList<DiscordChannelDto>> GetChannelsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordChannelDto>> GetCategoriesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DiscordRoleDto>> GetRolesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<RequestResourceSyncResponse?> RequestSyncAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPendingSyncDiscordGuildIdsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> SyncResourcesAsync(
        string discordGuildId,
        SyncResourcesRequest request,
        CancellationToken cancellationToken = default);
}

public class GuildResourceService : IGuildResourceService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogService _logService;

    public GuildResourceService(AppDbContext dbContext, ILogService logService)
    {
        _dbContext = dbContext;
        _logService = logService;
    }

    public async Task<IReadOnlyList<DiscordChannelDto>> GetChannelsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await FindOwnedGuildAsync(guildId, ownerDiscordUserId, cancellationToken);
        if (guild is null)
        {
            return [];
        }

        return await _dbContext.DiscordChannels
            .AsNoTracking()
            .Where(c => c.GuildId == guildId)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.Name)
            .Select(c => new DiscordChannelDto
            {
                DiscordChannelId = c.DiscordChannelId,
                Name = c.Name,
                Type = c.Type,
                Position = c.Position
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordChannelDto>> GetCategoriesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await FindOwnedGuildAsync(guildId, ownerDiscordUserId, cancellationToken);
        if (guild is null)
        {
            return [];
        }

        return await _dbContext.DiscordChannels
            .AsNoTracking()
            .Where(c => c.GuildId == guildId && c.Type == DiscordChannelType.Category)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.Name)
            .Select(c => new DiscordChannelDto
            {
                DiscordChannelId = c.DiscordChannelId,
                Name = c.Name,
                Type = c.Type,
                Position = c.Position
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DiscordRoleDto>> GetRolesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await FindOwnedGuildAsync(guildId, ownerDiscordUserId, cancellationToken);
        if (guild is null)
        {
            return [];
        }

        return await _dbContext.DiscordRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderByDescending(r => r.Position)
            .Select(r => new DiscordRoleDto
            {
                DiscordRoleId = r.DiscordRoleId,
                Name = r.Name,
                Color = r.Color,
                Position = r.Position,
                IsManaged = r.IsManaged
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<RequestResourceSyncResponse?> RequestSyncAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);

        if (guild is null)
        {
            return null;
        }

        guild.ResourceSyncRequested = true;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RequestResourceSyncResponse
        {
            Message = "Sync requested. The bot will update channels and roles shortly.",
            ResourcesSyncedAt = guild.ResourcesSyncedAt
        };
    }

    public async Task<IReadOnlyList<string>> GetPendingSyncDiscordGuildIdsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => g.IsActive && g.ResourceSyncRequested)
            .Select(g => g.DiscordGuildId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> SyncResourcesAsync(
        string discordGuildId,
        SyncResourcesRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return false;
        }

        var existingChannels = await _dbContext.DiscordChannels
            .Where(c => c.GuildId == guild.Id)
            .ToListAsync(cancellationToken);

        var existingRoles = await _dbContext.DiscordRoles
            .Where(r => r.GuildId == guild.Id)
            .ToListAsync(cancellationToken);

        _dbContext.DiscordChannels.RemoveRange(existingChannels);
        _dbContext.DiscordRoles.RemoveRange(existingRoles);

        foreach (var channel in request.Channels)
        {
            _dbContext.DiscordChannels.Add(new DiscordChannel
            {
                GuildId = guild.Id,
                DiscordChannelId = channel.DiscordChannelId,
                Name = channel.Name,
                Type = channel.Type,
                Position = channel.Position
            });
        }

        foreach (var role in request.Roles)
        {
            _dbContext.DiscordRoles.Add(new DiscordRole
            {
                GuildId = guild.Id,
                DiscordRoleId = role.DiscordRoleId,
                Name = role.Name,
                Color = role.Color,
                Position = role.Position,
                IsManaged = role.IsManaged
            });
        }

        guild.ResourceSyncRequested = false;
        guild.ResourcesSyncedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = discordGuildId,
            Type = LogEventType.ResourceSyncCompleted,
            Message = $"Synced {request.Channels.Count} channel(s) and {request.Roles.Count} role(s).",
            MetadataJson = LogService.BuildMetadataJson(new
            {
                channelCount = request.Channels.Count,
                roleCount = request.Roles.Count
            })
        }, cancellationToken);

        return true;
    }

    private async Task<Guild?> FindOwnedGuildAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);
    }
}
