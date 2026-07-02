using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface ITicketTimelineService
{
    Task<TicketTimelineEvent> AppendEventAsync(
        Ticket ticket,
        TicketTimelineEventType eventType,
        DateTimeOffset occurredAt,
        string? actorDiscordUserId = null,
        string? actorDisplayName = null,
        string? content = null,
        string? discordMessageId = null,
        Guid? relatedTimelineEventId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketTimelineEventDto>> GetTimelineAsync(
        Guid guildId,
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketTimelineEventDto>> GetTimelineForBotAsync(
        Guid ticketId,
        int? limit = null,
        CancellationToken cancellationToken = default);

    Task<TicketTimelineEventDto?> RecordMessageSentAsync(
        RecordTicketMessageSentRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketTimelineEventDto?> RecordArchivePostedAsync(
        Guid ticketId,
        RecordTicketArchivePostedRequest request,
        CancellationToken cancellationToken = default);
}

public class TicketTimelineService : ITicketTimelineService
{
    private readonly AppDbContext _dbContext;

    public TicketTimelineService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TicketTimelineEvent> AppendEventAsync(
        Ticket ticket,
        TicketTimelineEventType eventType,
        DateTimeOffset occurredAt,
        string? actorDiscordUserId = null,
        string? actorDisplayName = null,
        string? content = null,
        string? discordMessageId = null,
        Guid? relatedTimelineEventId = null,
        string? metadataJson = null,
        CancellationToken cancellationToken = default)
    {
        // D-001 §8, BR-T03: Timeline Events are append-only.
        var timelineEvent = new TicketTimelineEvent
        {
            TicketId = ticket.Id,
            GuildId = ticket.GuildId,
            EventType = eventType,
            OccurredAt = occurredAt,
            ActorDiscordUserId = actorDiscordUserId,
            ActorDisplayName = actorDisplayName,
            Content = content,
            DiscordMessageId = discordMessageId,
            RelatedTimelineEventId = relatedTimelineEventId,
            MetadataJson = metadataJson
        };

        _dbContext.TicketTimelineEvents.Add(timelineEvent);
        return Task.FromResult(timelineEvent);
    }

    public async Task<IReadOnlyList<TicketTimelineEventDto>> GetTimelineAsync(
        Guid guildId,
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var events = await _dbContext.TicketTimelineEvents
            .AsNoTracking()
            .Where(e => e.TicketId == ticketId && e.GuildId == guildId)
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken);

        return events.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<TicketTimelineEventDto>> GetTimelineForBotAsync(
        Guid ticketId,
        int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.TicketTimelineEvents
            .AsNoTracking()
            .Where(e => e.TicketId == ticketId)
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            .AsQueryable();

        if (limit is > 0)
        {
            query = query.Take(limit.Value);
        }

        var events = await query.ToListAsync(cancellationToken);
        events.Reverse();
        return events.Select(Map).ToList();
    }

    public async Task<TicketTimelineEventDto?> RecordMessageSentAsync(
        RecordTicketMessageSentRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(
                t => t.ChannelDiscordId == request.ChannelDiscordId,
                cancellationToken);

        if (ticket is null || ticket.Status != TicketStatus.Open)
        {
            // BR-S01: Only Open tickets accept new messages.
            return null;
        }

        var existing = await _dbContext.TicketTimelineEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.TicketId == ticket.Id && e.DiscordMessageId == request.DiscordMessageId,
                cancellationToken);

        if (existing)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return null;
        }

        var occurredAt = request.OccurredAt ?? DateTimeOffset.UtcNow;

        // BR-T01: Discord ticket channel messages become MessageSent Timeline Events.
        var timelineEvent = await AppendEventAsync(
            ticket,
            TicketTimelineEventType.MessageSent,
            occurredAt,
            request.AuthorDiscordUserId,
            request.AuthorDisplayName,
            request.Content.Trim(),
            request.DiscordMessageId,
            cancellationToken: cancellationToken);

        await MemberDisplayNameHelper.EnsureMemberKnownAsync(
            _dbContext,
            ticket.GuildId,
            request.AuthorDiscordUserId,
            request.AuthorDisplayName,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(timelineEvent);
    }

    public async Task<TicketTimelineEventDto?> RecordArchivePostedAsync(
        Guid ticketId,
        RecordTicketArchivePostedRequest request,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        // BR-T05: ArchivePosted is a system notification — actor optional (closed-by), not impersonating a message author.
        var metadata = TicketTimelineMetadataBuilder.BuildArchivePosted(request.ArchiveChannelDiscordId);

        var timelineEvent = await AppendEventAsync(
            ticket,
            TicketTimelineEventType.ArchivePosted,
            DateTimeOffset.UtcNow,
            request.ActorDiscordUserId,
            request.ActorDisplayName,
            metadataJson: metadata,
            cancellationToken: cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(timelineEvent);
    }

    internal static TicketTimelineEventDto Map(TicketTimelineEvent timelineEvent) =>
        new()
        {
            Id = timelineEvent.Id,
            TicketId = timelineEvent.TicketId,
            EventType = timelineEvent.EventType,
            OccurredAt = timelineEvent.OccurredAt,
            ActorDiscordUserId = timelineEvent.ActorDiscordUserId,
            ActorDisplayName = timelineEvent.ActorDisplayName,
            Content = timelineEvent.Content,
            RelatedTimelineEventId = timelineEvent.RelatedTimelineEventId,
            MetadataJson = timelineEvent.MetadataJson
        };
}

internal static class TicketTimelineMetadataBuilder
{
    public static string BuildTicketCreated(int ticketNumber, string channelDiscordId, string ownerDiscordUserId) =>
        LogService.BuildMetadataJson(new
        {
            ticketNumber,
            channelDiscordId,
            ownerDiscordUserId
        });

    public static string BuildStatusChanged(
        TicketStatus fromStatus,
        TicketStatus toStatus,
        string source,
        int ticketNumber) =>
        LogService.BuildMetadataJson(new
        {
            fromStatus = fromStatus.ToString(),
            toStatus = toStatus.ToString(),
            source,
            ticketNumber
        });

    public static string BuildArchivePosted(string archiveChannelDiscordId) =>
        LogService.BuildMetadataJson(new { archiveChannelDiscordId });
}
