using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface IModerationService
{
    Task<WarningDto?> CreateWarningAsync(
        CreateWarningRequest request,
        CancellationToken cancellationToken = default);

    Task<ModerationCaseDto?> CreateCaseAsync(
        CreateModerationCaseRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarningDto>> GetWarningsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        ModerationListFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModerationCaseDto>> GetCasesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        ModerationListFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarningDto>> GetWarningsByDiscordGuildAsync(
        string discordGuildId,
        string targetDiscordUserId,
        CancellationToken cancellationToken = default);
}

public class ModerationService : IModerationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogService _logService;
    private readonly IGuildAccessService _guildAccessService;

    public ModerationService(
        AppDbContext dbContext,
        ILogService logService,
        IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _logService = logService;
        _guildAccessService = guildAccessService;
    }

    public async Task<WarningDto?> CreateWarningAsync(
        CreateWarningRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.DiscordGuildId == request.DiscordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var warning = new Warning
        {
            GuildId = guild.Id,
            TargetDiscordUserId = request.TargetDiscordUserId,
            ModeratorDiscordUserId = request.ModeratorDiscordUserId,
            Reason = request.Reason.Trim()
        };

        _dbContext.Warnings.Add(warning);

        _dbContext.ModerationCases.Add(new ModerationCase
        {
            GuildId = guild.Id,
            Type = ModerationCaseType.Warn,
            TargetDiscordUserId = request.TargetDiscordUserId,
            ModeratorDiscordUserId = request.ModeratorDiscordUserId,
            Reason = request.Reason.Trim()
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = request.DiscordGuildId,
            Type = LogEventType.WarningCreated,
            Message = $"Warning issued: {request.Reason.Trim()}",
            ActorDiscordUserId = request.ModeratorDiscordUserId,
            TargetDiscordUserId = request.TargetDiscordUserId
        }, cancellationToken);

        return MapWarning(warning);
    }

    public async Task<ModerationCaseDto?> CreateCaseAsync(
        CreateModerationCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .FirstOrDefaultAsync(g => g.DiscordGuildId == request.DiscordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return null;
        }

        var moderationCase = new ModerationCase
        {
            GuildId = guild.Id,
            Type = request.Type,
            TargetDiscordUserId = request.TargetDiscordUserId,
            ModeratorDiscordUserId = request.ModeratorDiscordUserId,
            Reason = request.Reason?.Trim(),
            MessageCount = request.MessageCount,
            ChannelDiscordId = request.ChannelDiscordId
        };

        _dbContext.ModerationCases.Add(moderationCase);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await WriteCaseLogAsync(request, moderationCase, cancellationToken);

        return MapCase(moderationCase);
    }

    private async Task WriteCaseLogAsync(
        CreateModerationCaseRequest request,
        ModerationCase moderationCase,
        CancellationToken cancellationToken)
    {
        var (type, message) = request.Type switch
        {
            ModerationCaseType.Kick => (
                LogEventType.MemberKicked,
                $"Member kicked{(string.IsNullOrWhiteSpace(request.Reason) ? "." : $": {request.Reason.Trim()}")}"),
            ModerationCaseType.Clear => (
                LogEventType.MessagesCleared,
                $"Cleared {request.MessageCount ?? 0} message(s)."),
            _ => (LogEventType.WarningCreated, "Moderation case recorded.")
        };

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = request.DiscordGuildId,
            Type = type,
            Message = message,
            ActorDiscordUserId = request.ModeratorDiscordUserId,
            TargetDiscordUserId = request.TargetDiscordUserId,
            ChannelDiscordId = request.ChannelDiscordId,
            MetadataJson = request.Type == ModerationCaseType.Clear
                ? LogService.BuildMetadataJson(new { request.MessageCount })
                : null
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<WarningDto>> GetWarningsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        ModerationListFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanAccessModerationPagesAsync(
                guildId, ownerDiscordUserId, cancellationToken))
        {
            return [];
        }

        var query = _dbContext.Warnings
            .AsNoTracking()
            .Where(w => w.GuildId == guildId);

        query = ApplyWarningFilters(query, filter);

        return await query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WarningDto
            {
                Id = w.Id,
                TargetDiscordUserId = w.TargetDiscordUserId,
                ModeratorDiscordUserId = w.ModeratorDiscordUserId,
                Reason = w.Reason,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModerationCaseDto>> GetCasesAsync(
        Guid guildId,
        string ownerDiscordUserId,
        ModerationListFilter filter,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanAccessModerationPagesAsync(
                guildId, ownerDiscordUserId, cancellationToken))
        {
            return [];
        }

        var query = _dbContext.ModerationCases
            .AsNoTracking()
            .Where(c => c.GuildId == guildId);

        query = ApplyCaseFilters(query, filter);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ModerationCaseDto
            {
                Id = c.Id,
                Type = c.Type,
                TargetDiscordUserId = c.TargetDiscordUserId,
                ModeratorDiscordUserId = c.ModeratorDiscordUserId,
                Reason = c.Reason,
                MessageCount = c.MessageCount,
                ChannelDiscordId = c.ChannelDiscordId,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WarningDto>> GetWarningsByDiscordGuildAsync(
        string discordGuildId,
        string targetDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Warnings
            .AsNoTracking()
            .Where(w => w.Guild.DiscordGuildId == discordGuildId
                        && w.Guild.IsActive
                        && w.TargetDiscordUserId == targetDiscordUserId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WarningDto
            {
                Id = w.Id,
                TargetDiscordUserId = w.TargetDiscordUserId,
                ModeratorDiscordUserId = w.ModeratorDiscordUserId,
                Reason = w.Reason,
                CreatedAt = w.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Warning> ApplyWarningFilters(IQueryable<Warning> query, ModerationListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.TargetUserId))
        {
            query = query.Where(w => w.TargetDiscordUserId == filter.TargetUserId);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(w => w.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(w => w.CreatedAt <= filter.To.Value);
        }

        return query;
    }

    private static IQueryable<ModerationCase> ApplyCaseFilters(IQueryable<ModerationCase> query, ModerationListFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.TargetUserId))
        {
            query = query.Where(c => c.TargetDiscordUserId == filter.TargetUserId);
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(c => c.Type == filter.Type.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(c => c.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(c => c.CreatedAt <= filter.To.Value);
        }

        return query;
    }

    private static WarningDto MapWarning(Warning warning) =>
        new()
        {
            Id = warning.Id,
            TargetDiscordUserId = warning.TargetDiscordUserId,
            ModeratorDiscordUserId = warning.ModeratorDiscordUserId,
            Reason = warning.Reason,
            CreatedAt = warning.CreatedAt
        };

    private static ModerationCaseDto MapCase(ModerationCase moderationCase) =>
        new()
        {
            Id = moderationCase.Id,
            Type = moderationCase.Type,
            TargetDiscordUserId = moderationCase.TargetDiscordUserId,
            ModeratorDiscordUserId = moderationCase.ModeratorDiscordUserId,
            Reason = moderationCase.Reason,
            MessageCount = moderationCase.MessageCount,
            ChannelDiscordId = moderationCase.ChannelDiscordId,
            CreatedAt = moderationCase.CreatedAt
        };
}
