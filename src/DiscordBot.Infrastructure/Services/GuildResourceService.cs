using System.Text.Json;
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

    Task<IReadOnlyList<DiscordGuildMemberDto>> GetMembersAsync(
        Guid guildId,
        string discordUserId,
        string? search,
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
    private readonly IGuildAccessService _guildAccessService;

    public GuildResourceService(
        AppDbContext dbContext,
        ILogService logService,
        IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _logService = logService;
        _guildAccessService = guildAccessService;
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

    public async Task<IReadOnlyList<DiscordGuildMemberDto>> GetMembersAsync(
        Guid guildId,
        string discordUserId,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var access = await _guildAccessService.GetAccessAsync(guildId, discordUserId, cancellationToken);
        if (access is null)
        {
            return [];
        }

        var query = _dbContext.DiscordGuildMembers
            .AsNoTracking()
            .Where(m => m.GuildId == guildId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.DiscordUserId, term)
                || EF.Functions.ILike(m.Username, term)
                || (m.GlobalName != null && EF.Functions.ILike(m.GlobalName, term))
                || (m.Nickname != null && EF.Functions.ILike(m.Nickname, term)));
        }

        return await query
            .OrderBy(m => m.GlobalName ?? m.Nickname ?? m.Username)
            .ThenBy(m => m.Username)
            .Take(100)
            .Select(m => new DiscordGuildMemberDto
            {
                DiscordUserId = m.DiscordUserId,
                Username = m.Username,
                GlobalName = m.GlobalName,
                Nickname = m.Nickname,
                DisplayName = m.Nickname ?? m.GlobalName ?? m.Username
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
            Message = "Sync requested. The bot will update channels, roles, and members shortly.",
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

        var existingMembers = await _dbContext.DiscordGuildMembers
            .Where(m => m.GuildId == guild.Id)
            .ToListAsync(cancellationToken);

        _dbContext.DiscordChannels.RemoveRange(existingChannels);
        _dbContext.DiscordRoles.RemoveRange(existingRoles);
        _dbContext.DiscordGuildMembers.RemoveRange(existingMembers);

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

        foreach (var member in request.Members)
        {
            _dbContext.DiscordGuildMembers.Add(new DiscordGuildMember
            {
                GuildId = guild.Id,
                DiscordUserId = member.DiscordUserId,
                Username = member.Username,
                GlobalName = member.GlobalName,
                Nickname = member.Nickname,
                DiscordRoleIdsJson = JsonSerializer.Serialize(member.DiscordRoleIds)
            });
        }

        guild.ResourceSyncRequested = false;
        guild.ResourcesSyncedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = discordGuildId,
            Type = LogEventType.ResourceSyncCompleted,
            Message = $"Synced {request.Channels.Count} channel(s), {request.Roles.Count} role(s), and {request.Members.Count} member(s).",
            MetadataJson = LogService.BuildMetadataJson(new
            {
                channelCount = request.Channels.Count,
                roleCount = request.Roles.Count,
                memberCount = request.Members.Count
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
