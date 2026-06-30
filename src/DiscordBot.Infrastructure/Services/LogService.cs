using System.Text.Json;
using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Domain.Extensions;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface ILogService
{
    Task<LogEntryDto?> CreateLogAsync(
        CreateLogRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LogEntryDto>> GetLogsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        LogListFilter filter,
        CancellationToken cancellationToken = default);
}

public class LogService : ILogService
{
    private readonly AppDbContext _dbContext;

    public LogService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LogEntryDto?> CreateLogAsync(
        CreateLogRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return null;
        }

        var guild = await ResolveGuildAsync(request, cancellationToken);
        if (guild is null)
        {
            return null;
        }

        if (!await ShouldWriteAsync(guild.Id, request.Type, cancellationToken))
        {
            return null;
        }

        var entry = new LogEntry
        {
            GuildId = guild.Id,
            Type = request.Type,
            Message = request.Message.Trim(),
            ActorDiscordUserId = request.ActorDiscordUserId,
            TargetDiscordUserId = request.TargetDiscordUserId,
            ChannelDiscordId = request.ChannelDiscordId,
            MetadataJson = request.MetadataJson
        };

        _dbContext.LogEntries.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Map(entry);
    }

    public async Task<IReadOnlyList<LogEntryDto>> GetLogsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        LogListFilter filter,
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

        var query = _dbContext.LogEntries
            .AsNoTracking()
            .Where(l => l.GuildId == guildId);

        if (filter.Type.HasValue)
        {
            query = query.Where(l => l.Type == filter.Type.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= filter.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(l =>
                l.Message.Contains(term)
                || (l.ActorDiscordUserId != null && l.ActorDiscordUserId.Contains(term))
                || (l.TargetDiscordUserId != null && l.TargetDiscordUserId.Contains(term))
                || (l.ChannelDiscordId != null && l.ChannelDiscordId.Contains(term))
                || (l.MetadataJson != null && l.MetadataJson.Contains(term)));
        }

        var entries = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return entries.Select(Map).ToList();
    }

    private async Task<Guild?> ResolveGuildAsync(
        CreateLogRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DiscordGuildId))
        {
            return null;
        }

        return await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(
                g => g.DiscordGuildId == request.DiscordGuildId && g.IsActive,
                cancellationToken);
    }

    private async Task<bool> ShouldWriteAsync(
        Guid guildId,
        LogEventType type,
        CancellationToken cancellationToken)
    {
        if (LogEventTypeExtensions.IsCritical(type))
        {
            return true;
        }

        return await _dbContext.GuildModules
            .AsNoTracking()
            .AnyAsync(
                gm => gm.GuildId == guildId
                      && gm.Module.Key == ModuleKeys.Logs
                      && gm.IsEnabled,
                cancellationToken);
    }

    private static LogEntryDto Map(LogEntry entry) =>
        new()
        {
            Id = entry.Id,
            Type = entry.Type,
            TypeLabel = LogEventTypeExtensions.GetLabel(entry.Type),
            Message = entry.Message,
            ActorDiscordUserId = entry.ActorDiscordUserId,
            TargetDiscordUserId = entry.TargetDiscordUserId,
            ChannelDiscordId = entry.ChannelDiscordId,
            MetadataJson = entry.MetadataJson,
            CreatedAt = entry.CreatedAt
        };

    internal static string BuildMetadataJson(object metadata) =>
        JsonSerializer.Serialize(metadata);
}
