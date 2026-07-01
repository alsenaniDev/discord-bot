using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IGuildStaffService
{
    Task<IReadOnlyList<GuildStaffDto>> GetStaffAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<GuildStaffDto?> AddStaffAsync(
        Guid guildId,
        string discordUserId,
        AddGuildStaffRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> RemoveStaffAsync(
        Guid guildId,
        Guid staffId,
        string discordUserId,
        CancellationToken cancellationToken = default);
}

public class GuildStaffService : IGuildStaffService
{
    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;

    public GuildStaffService(AppDbContext dbContext, IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
    }

    public async Task<IReadOnlyList<GuildStaffDto>> GetStaffAsync(
        Guid guildId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanManageStaffAsync(guildId, discordUserId, cancellationToken))
        {
            return [];
        }

        return await _dbContext.GuildStaff
            .AsNoTracking()
            .Where(s => s.GuildId == guildId)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new GuildStaffDto
            {
                Id = s.Id,
                GuildId = s.GuildId,
                DiscordUserId = s.DiscordUserId,
                Role = s.Role,
                CreatedAt = s.CreatedAt,
                CreatedByDiscordUserId = s.CreatedByDiscordUserId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GuildStaffDto?> AddStaffAsync(
        Guid guildId,
        string discordUserId,
        AddGuildStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanManageStaffAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var targetDiscordUserId = request.DiscordUserId.Trim();
        if (string.IsNullOrWhiteSpace(targetDiscordUserId))
        {
            return null;
        }

        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guildId && g.IsActive, cancellationToken);

        if (guild is null || guild.OwnerDiscordUserId == targetDiscordUserId)
        {
            return null;
        }

        var exists = await _dbContext.GuildStaff
            .AnyAsync(
                s => s.GuildId == guildId && s.DiscordUserId == targetDiscordUserId,
                cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("This user is already staff for the guild.");
        }

        var staff = new GuildStaff
        {
            GuildId = guildId,
            DiscordUserId = targetDiscordUserId,
            Role = request.Role,
            CreatedByDiscordUserId = discordUserId
        };

        _dbContext.GuildStaff.Add(staff);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GuildStaffDto
        {
            Id = staff.Id,
            GuildId = staff.GuildId,
            DiscordUserId = staff.DiscordUserId,
            Role = staff.Role,
            CreatedAt = staff.CreatedAt,
            CreatedByDiscordUserId = staff.CreatedByDiscordUserId
        };
    }

    public async Task<bool> RemoveStaffAsync(
        Guid guildId,
        Guid staffId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanManageStaffAsync(guildId, discordUserId, cancellationToken))
        {
            return false;
        }

        var staff = await _dbContext.GuildStaff
            .FirstOrDefaultAsync(s => s.Id == staffId && s.GuildId == guildId, cancellationToken);

        if (staff is null)
        {
            return false;
        }

        _dbContext.GuildStaff.Remove(staff);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
