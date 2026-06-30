using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IReactionRoleService
{
    Task<ReactionRoleDto?> CreateAsync(
        CreateReactionRoleRequest request,
        CancellationToken cancellationToken = default);

    Task<ReactionRoleDto?> GetByButtonCustomIdAsync(
        string buttonCustomId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReactionRoleDto>> GetGuildReactionRolesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<bool> DeactivateAsync(
        Guid guildId,
        Guid reactionRoleId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);
}

public class ReactionRoleService : IReactionRoleService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogService _logService;

    public ReactionRoleService(AppDbContext dbContext, ILogService logService)
    {
        _dbContext = dbContext;
        _logService = logService;
    }

    public async Task<ReactionRoleDto?> CreateAsync(
        CreateReactionRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.DiscordGuildId == request.DiscordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var exists = await _dbContext.ReactionRoles
            .AnyAsync(r => r.ButtonCustomId == request.ButtonCustomId, cancellationToken);

        if (exists)
        {
            return null;
        }

        var reactionRole = new ReactionRole
        {
            GuildId = guild.Id,
            ChannelDiscordId = request.ChannelDiscordId,
            MessageDiscordId = request.MessageDiscordId,
            RoleDiscordId = request.RoleDiscordId,
            ButtonCustomId = request.ButtonCustomId,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            ButtonLabel = request.ButtonLabel.Trim(),
            IsActive = true
        };

        _dbContext.ReactionRoles.Add(reactionRole);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = request.DiscordGuildId,
            Type = LogEventType.ReactionRoleCreated,
            Message = $"Reaction role panel \"{reactionRole.Title}\" created.",
            ActorDiscordUserId = request.CreatedByDiscordUserId,
            ChannelDiscordId = reactionRole.ChannelDiscordId,
            MetadataJson = LogService.BuildMetadataJson(new
            {
                reactionRoleId = reactionRole.Id,
                roleDiscordId = reactionRole.RoleDiscordId
            })
        }, cancellationToken);

        return Map(reactionRole);
    }

    public async Task<ReactionRoleDto?> GetByButtonCustomIdAsync(
        string buttonCustomId,
        CancellationToken cancellationToken = default)
    {
        var reactionRole = await _dbContext.ReactionRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.ButtonCustomId == buttonCustomId, cancellationToken);

        return reactionRole is null ? null : Map(reactionRole);
    }

    public async Task<IReadOnlyList<ReactionRoleDto>> GetGuildReactionRolesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var ownsGuild = await _dbContext.Guilds
            .AsNoTracking()
            .AnyAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);

        if (!ownsGuild)
        {
            return [];
        }

        var items = await _dbContext.ReactionRoles
            .AsNoTracking()
            .Where(r => r.GuildId == guildId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(Map).ToList();
    }

    public async Task<bool> DeactivateAsync(
        Guid guildId,
        Guid reactionRoleId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(
                g => g.Id == guildId && g.OwnerDiscordUserId == ownerDiscordUserId && g.IsActive,
                cancellationToken);

        if (guild is null)
        {
            return false;
        }

        var reactionRole = await _dbContext.ReactionRoles
            .FirstOrDefaultAsync(r => r.Id == reactionRoleId && r.GuildId == guildId, cancellationToken);

        if (reactionRole is null || !reactionRole.IsActive)
        {
            return false;
        }

        reactionRole.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = guild.DiscordGuildId,
            Type = LogEventType.ReactionRoleDeleted,
            Message = $"Reaction role panel \"{reactionRole.Title}\" deactivated.",
            ActorDiscordUserId = ownerDiscordUserId,
            ChannelDiscordId = reactionRole.ChannelDiscordId,
            MetadataJson = LogService.BuildMetadataJson(new
            {
                reactionRoleId = reactionRole.Id,
                roleDiscordId = reactionRole.RoleDiscordId
            })
        }, cancellationToken);

        return true;
    }

    private static ReactionRoleDto Map(ReactionRole reactionRole) =>
        new()
        {
            Id = reactionRole.Id,
            GuildId = reactionRole.GuildId,
            ChannelDiscordId = reactionRole.ChannelDiscordId,
            MessageDiscordId = reactionRole.MessageDiscordId,
            RoleDiscordId = reactionRole.RoleDiscordId,
            ButtonCustomId = reactionRole.ButtonCustomId,
            Title = reactionRole.Title,
            Description = reactionRole.Description,
            ButtonLabel = reactionRole.ButtonLabel,
            IsActive = reactionRole.IsActive,
            CreatedAt = reactionRole.CreatedAt
        };
}
