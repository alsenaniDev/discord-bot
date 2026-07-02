using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface ITicketReadService
{
    Task<PaginatedTicketSummaryReadModel?> GetTicketSummariesAsync(
        Guid guildId,
        string discordUserId,
        TicketSummaryQuery query,
        CancellationToken cancellationToken = default);

    Task<PaginatedTicketConversationReadModel?> GetTicketConversationAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        TicketConversationQuery query,
        CancellationToken cancellationToken = default);

    Task<PaginatedTicketConversationReadModel?> GetTicketConversationForBotAsync(
        Guid ticketId,
        TicketConversationQuery query,
        CancellationToken cancellationToken = default);

    Task<TicketTranscriptReadModel?> GetTicketTranscriptAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        TicketTranscriptQuery query,
        CancellationToken cancellationToken = default);
}

public class TicketReadService : ITicketReadService
{
    private const int MaxPageSize = 100;
    private const int MaxConversationLimit = 200;
    private const int PreviewMaxLength = 160;

    private readonly AppDbContext _dbContext;
    private readonly IGuildAccessService _guildAccessService;

    public TicketReadService(AppDbContext dbContext, IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _guildAccessService = guildAccessService;
    }

    public async Task<PaginatedTicketSummaryReadModel?> GetTicketSummariesAsync(
        Guid guildId,
        string discordUserId,
        TicketSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanViewTicketsAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize < 1 ? 20 : Math.Min(query.PageSize, MaxPageSize);

        var ticketsQuery = _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.GuildId == guildId);

        if (query.Status is not null)
        {
            ticketsQuery = ticketsQuery.Where(t => t.Status == query.Status);
        }

        var totalCount = await ticketsQuery.CountAsync(cancellationToken);
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        var sort = (query.Sort ?? "lastActivity").Trim().ToLowerInvariant();
        ticketsQuery = sort switch
        {
            "created" => ticketsQuery.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.TicketNumber),
            "number" => ticketsQuery.OrderByDescending(t => t.TicketNumber),
            _ => ticketsQuery.OrderByDescending(t =>
                    _dbContext.TicketTimelineEvents
                        .Where(e => e.TicketId == t.Id)
                        .Max(e => (DateTimeOffset?)e.OccurredAt) ?? t.CreatedAt)
                .ThenByDescending(t => t.TicketNumber)
        };

        var tickets = await ticketsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (tickets.Count == 0)
        {
            return new PaginatedTicketSummaryReadModel
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }

        var ticketIds = tickets.Select(t => t.Id).ToList();

        var stats = await _dbContext.TicketTimelineEvents
            .AsNoTracking()
            .Where(e => ticketIds.Contains(e.TicketId))
            .GroupBy(e => e.TicketId)
            .Select(g => new
            {
                TicketId = g.Key,
                LastActivityAt = g.Max(e => e.OccurredAt),
                MessageCount = g.Count(e => e.EventType == TicketTimelineEventType.MessageSent),
                StaffReplyCount = g.Count(e => e.EventType == TicketTimelineEventType.StaffReplyQueued),
                FailedDeliveryCount = g.Count(e => e.EventType == TicketTimelineEventType.StaffReplyFailed)
            })
            .ToDictionaryAsync(x => x.TicketId, cancellationToken);

        var previewCandidates = await _dbContext.TicketTimelineEvents
            .AsNoTracking()
            .Where(e => ticketIds.Contains(e.TicketId) && e.Content != null && e.Content != string.Empty)
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            .Select(e => new { e.TicketId, e.Content })
            .ToListAsync(cancellationToken);

        var previews = previewCandidates
            .GroupBy(e => e.TicketId)
            .ToDictionary(g => g.Key, g => TruncatePreview(g.First().Content!));

        var ownerNames = await MemberDisplayNameHelper.ResolveMemberNamesAsync(
            _dbContext,
            guildId,
            tickets.Select(t => t.OwnerDiscordUserId),
            cancellationToken);

        var items = tickets.Select(ticket =>
        {
            stats.TryGetValue(ticket.Id, out var ticketStats);
            previews.TryGetValue(ticket.Id, out var preview);

            return new TicketSummaryReadModel
            {
                TicketId = ticket.Id,
                GuildId = ticket.GuildId,
                TicketNumber = ticket.TicketNumber,
                OwnerDiscordId = ticket.OwnerDiscordUserId,
                OwnerUsername = ownerNames.GetValueOrDefault(ticket.OwnerDiscordUserId),
                Status = ticket.Status,
                DiscordChannelId = ticket.ChannelDiscordId,
                CreatedAt = ticket.CreatedAt,
                ClosedAt = ticket.ClosedAt,
                LastActivityAt = ticketStats?.LastActivityAt ?? ticket.ClosedAt ?? ticket.CreatedAt,
                LastMessagePreview = preview,
                MessageCount = ticketStats?.MessageCount ?? 0,
                StaffReplyCount = ticketStats?.StaffReplyCount ?? 0,
                FailedDeliveryCount = ticketStats?.FailedDeliveryCount ?? 0
            };
        }).ToList();

        return new PaginatedTicketSummaryReadModel
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<PaginatedTicketConversationReadModel?> GetTicketConversationAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        TicketConversationQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanViewTicketsAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        return await BuildTicketConversationAsync(guildId, ticketId, query, cancellationToken);
    }

    public async Task<PaginatedTicketConversationReadModel?> GetTicketConversationForBotAsync(
        Guid ticketId,
        TicketConversationQuery query,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        return await BuildTicketConversationAsync(ticket.GuildId, ticketId, query, cancellationToken);
    }

    public async Task<TicketTranscriptReadModel?> GetTicketTranscriptAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        TicketTranscriptQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await _guildAccessService.CanViewTicketsAsync(guildId, discordUserId, cancellationToken))
        {
            return null;
        }

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.GuildId == guildId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var canSeeInternalNotes = await _guildAccessService.CanReplyToTicketsAsync(
            guildId,
            discordUserId,
            cancellationToken);

        var conversationQuery = new TicketConversationQuery
        {
            CursorOccurredAt = query.CursorOccurredAt,
            CursorEventId = query.CursorEventId,
            Limit = query.Limit
        };

        var conversation = await BuildTicketConversationAsync(
            guildId,
            ticketId,
            conversationQuery,
            cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var ownerNames = await MemberDisplayNameHelper.ResolveMemberNamesAsync(
            _dbContext,
            guildId,
            [ticket.OwnerDiscordUserId],
            cancellationToken);

        var entries = canSeeInternalNotes
            ? conversation.Items
            : conversation.Items.Where(e => !e.IsInternal).ToList();

        return new TicketTranscriptReadModel
        {
            Metadata = new TicketTranscriptMetadataReadModel
            {
                TicketId = ticket.Id,
                GuildId = ticket.GuildId,
                TicketNumber = ticket.TicketNumber,
                OwnerDiscordId = ticket.OwnerDiscordUserId,
                OwnerUsername = ownerNames.GetValueOrDefault(ticket.OwnerDiscordUserId),
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                ClosedAt = ticket.ClosedAt
            },
            Entries = entries,
            HasMore = conversation.HasMore,
            NextCursorOccurredAt = conversation.NextCursorOccurredAt,
            NextCursorEventId = conversation.NextCursorEventId
        };
    }

    private async Task<PaginatedTicketConversationReadModel?> BuildTicketConversationAsync(
        Guid guildId,
        Guid ticketId,
        TicketConversationQuery query,
        CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.GuildId == guildId, cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var limit = query.Limit < 1 ? 50 : Math.Min(query.Limit, MaxConversationLimit);

        var eventsQuery = _dbContext.TicketTimelineEvents
            .AsNoTracking()
            .Where(e => e.TicketId == ticketId && e.GuildId == guildId);

        if (query.CursorOccurredAt is not null && query.CursorEventId is not null)
        {
            var cursorOccurredAt = query.CursorOccurredAt.Value;
            var cursorEventId = query.CursorEventId.Value;

            eventsQuery = eventsQuery.Where(e =>
                e.OccurredAt > cursorOccurredAt
                || (e.OccurredAt == cursorOccurredAt && e.Id.CompareTo(cursorEventId) > 0));
        }

        var events = await eventsQuery
            .OrderBy(e => e.OccurredAt)
            .ThenBy(e => e.Id)
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        var hasMore = events.Count > limit;
        if (hasMore)
        {
            events = events.Take(limit).ToList();
        }

        var relatedQueuedIds = events
            .Where(e => e.RelatedTimelineEventId.HasValue
                && (e.EventType == TicketTimelineEventType.StaffReplyDelivered
                    || e.EventType == TicketTimelineEventType.StaffReplyFailed))
            .Select(e => e.RelatedTimelineEventId!.Value)
            .Distinct()
            .ToList();

        var queuedContentById = relatedQueuedIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await _dbContext.TicketTimelineEvents
                .AsNoTracking()
                .Where(e => relatedQueuedIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Content, cancellationToken);

        var items = events
            .Select(e => MapConversationEntry(e, ticket.OwnerDiscordUserId, queuedContentById))
            .ToList();

        DateTimeOffset? nextCursorOccurredAt = null;
        Guid? nextCursorEventId = null;
        if (hasMore && events.Count > 0)
        {
            var last = events[^1];
            nextCursorOccurredAt = last.OccurredAt;
            nextCursorEventId = last.Id;
        }

        return new PaginatedTicketConversationReadModel
        {
            Items = items,
            HasMore = hasMore,
            NextCursorOccurredAt = nextCursorOccurredAt,
            NextCursorEventId = nextCursorEventId
        };
    }

    internal static TicketConversationEntryReadModel MapConversationEntry(
        Domain.Entities.TicketTimelineEvent timelineEvent,
        string ownerDiscordUserId,
        IReadOnlyDictionary<Guid, string?> queuedContentById)
    {
        var deliveryStatus = timelineEvent.EventType switch
        {
            TicketTimelineEventType.StaffReplyQueued => TicketDeliveryStatus.Queued,
            TicketTimelineEventType.StaffReplyDelivered => TicketDeliveryStatus.Delivered,
            TicketTimelineEventType.StaffReplyFailed => TicketDeliveryStatus.Failed,
            _ => TicketDeliveryStatus.None
        };

        var actorType = ResolveActorType(timelineEvent, ownerDiscordUserId);
        var content = ResolveContent(timelineEvent, queuedContentById);

        return new TicketConversationEntryReadModel
        {
            EventId = timelineEvent.Id,
            TicketId = timelineEvent.TicketId,
            EventType = timelineEvent.EventType,
            ActorType = actorType,
            ActorDiscordId = timelineEvent.ActorDiscordUserId,
            ActorUsername = timelineEvent.ActorDisplayName,
            Content = content,
            IsInternal = false,
            DeliveryStatus = deliveryStatus,
            OccurredAt = timelineEvent.OccurredAt,
            CreatedAt = timelineEvent.CreatedAt
        };
    }

    private static TicketConversationActorType ResolveActorType(
        Domain.Entities.TicketTimelineEvent timelineEvent,
        string ownerDiscordUserId)
    {
        return timelineEvent.EventType switch
        {
            TicketTimelineEventType.TicketCreated => TicketConversationActorType.System,
            TicketTimelineEventType.StatusChanged => TicketConversationActorType.System,
            TicketTimelineEventType.ArchivePosted => TicketConversationActorType.System,
            TicketTimelineEventType.MessageSent when string.Equals(
                timelineEvent.ActorDiscordUserId,
                ownerDiscordUserId,
                StringComparison.Ordinal) => TicketConversationActorType.Owner,
            TicketTimelineEventType.MessageSent => TicketConversationActorType.Staff,
            TicketTimelineEventType.StaffReplyQueued
                or TicketTimelineEventType.StaffReplyDelivered
                or TicketTimelineEventType.StaffReplyFailed => TicketConversationActorType.Staff,
            _ => TicketConversationActorType.System
        };
    }

    private static string? ResolveContent(
        Domain.Entities.TicketTimelineEvent timelineEvent,
        IReadOnlyDictionary<Guid, string?> queuedContentById)
    {
        if (!string.IsNullOrWhiteSpace(timelineEvent.Content))
        {
            return timelineEvent.Content;
        }

        if (timelineEvent.RelatedTimelineEventId is null)
        {
            return timelineEvent.EventType switch
            {
                TicketTimelineEventType.TicketCreated => "Ticket opened.",
                TicketTimelineEventType.StatusChanged => "Ticket status changed.",
                TicketTimelineEventType.ArchivePosted => "Archive posted.",
                TicketTimelineEventType.StaffReplyDelivered => "Staff reply delivered to Discord.",
                _ => null
            };
        }

        if (queuedContentById.TryGetValue(timelineEvent.RelatedTimelineEventId.Value, out var queuedContent)
            && !string.IsNullOrWhiteSpace(queuedContent))
        {
            return queuedContent;
        }

        return timelineEvent.EventType switch
        {
            TicketTimelineEventType.StaffReplyDelivered => "Staff reply delivered to Discord.",
            TicketTimelineEventType.StaffReplyFailed => timelineEvent.Content ?? "Staff reply delivery failed.",
            _ => null
        };
    }

    private static string TruncatePreview(string value) =>
        value.Length <= PreviewMaxLength ? value : value[..PreviewMaxLength] + "…";
}
