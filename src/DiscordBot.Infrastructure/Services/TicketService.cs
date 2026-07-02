using DiscordBot.Domain.Constants;
using DiscordBot.Domain.Entities;
using DiscordBot.Domain.Enums;
using DiscordBot.Infrastructure.Data;
using DiscordBot.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace DiscordBot.Infrastructure.Services;

public interface ITicketService
{
    Task<bool> SetupTicketsAsync(string discordGuildId, string ticketCategoryId, CancellationToken cancellationToken = default);

    Task<TicketDto?> CreateTicketAsync(CreateTicketRequest request, CancellationToken cancellationToken = default);

    Task<TicketDto?> GetByChannelDiscordIdAsync(string channelDiscordId, CancellationToken cancellationToken = default);

    Task<TicketDto?> CloseTicketAsync(
        Guid ticketId,
        CloseTicketRequest? request = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetGuildTicketsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default);

    Task<TicketDto?> CloseTicketForGuildAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketChannelCleanupDto>> GetPendingChannelCleanupsAsync(
        CancellationToken cancellationToken = default);

    Task<bool> AcknowledgeChannelCleanupAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<TicketOutboundMessageDto?> SendTicketMessageAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        SendTicketMessageRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingTicketMessageDto>> GetPendingOutboundMessagesAsync(
        CancellationToken cancellationToken = default);

    Task<bool> AcknowledgeOutboundMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default);
}

public class TicketService : ITicketService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogService _logService;
    private readonly IGuildAccessService _guildAccessService;

    public TicketService(
        AppDbContext dbContext,
        ILogService logService,
        IGuildAccessService guildAccessService)
    {
        _dbContext = dbContext;
        _logService = logService;
        _guildAccessService = guildAccessService;
    }

    public async Task<bool> SetupTicketsAsync(
        string discordGuildId,
        string ticketCategoryId,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(g => g.DiscordGuildId == discordGuildId && g.IsActive, cancellationToken);

        if (guild is null)
        {
            return false;
        }

        if (guild.Settings is null)
        {
            guild.Settings = new GuildSettings { GuildId = guild.Id };
            _dbContext.GuildSettings.Add(guild.Settings);
        }

        guild.Settings.TicketsEnabled = true;
        guild.Settings.TicketCategoryId = ticketCategoryId;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TicketDto?> CreateTicketAsync(
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        var guild = await _dbContext.Guilds
            .Include(g => g.Settings)
            .FirstOrDefaultAsync(g => g.DiscordGuildId == request.DiscordGuildId && g.IsActive, cancellationToken);

        if (guild?.Settings is null || !guild.Settings.TicketsEnabled)
        {
            return null;
        }

        var hasOpenTicket = await _dbContext.Tickets.AnyAsync(
            t => t.GuildId == guild.Id
                 && t.OwnerDiscordUserId == request.OwnerDiscordUserId
                 && t.Status == TicketStatus.Open,
            cancellationToken);

        if (hasOpenTicket)
        {
            throw new InvalidOperationException("You already have an open ticket.");
        }

        var nextNumber = await _dbContext.Tickets
            .Where(t => t.GuildId == guild.Id)
            .MaxAsync(t => (int?)t.TicketNumber, cancellationToken) ?? 0;

        var ticket = new Ticket
        {
            GuildId = guild.Id,
            TicketNumber = nextNumber + 1,
            OwnerDiscordUserId = request.OwnerDiscordUserId,
            ChannelDiscordId = request.ChannelDiscordId,
            Status = TicketStatus.Open
        };

        _dbContext.Tickets.Add(ticket);

        await MemberDisplayNameHelper.EnsureMemberKnownAsync(
            _dbContext,
            guild.Id,
            request.OwnerDiscordUserId,
            request.OwnerDisplayName,
            cancellationToken);

        await MemberDisplayNameHelper.EnsureChannelKnownAsync(
            _dbContext,
            guild.Id,
            request.ChannelDiscordId,
            request.ChannelDisplayName,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = request.DiscordGuildId,
            Type = LogEventType.TicketOpened,
            Message = $"Ticket #{ticket.TicketNumber} opened.",
            ActorDiscordUserId = request.OwnerDiscordUserId,
            ChannelDiscordId = request.ChannelDiscordId,
            ActorDisplayName = request.OwnerDisplayName,
            ChannelDisplayName = request.ChannelDisplayName,
            MetadataJson = LogService.BuildMetadataJson(new { ticket.TicketNumber })
        }, cancellationToken);

        return (await EnrichTicketsAsync(guild.Id, [ticket], cancellationToken))[0];
    }

    public async Task<TicketDto?> GetByChannelDiscordIdAsync(
        string channelDiscordId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.ChannelDiscordId == channelDiscordId, cancellationToken);

        return ticket is null ? null : Map(ticket);
    }

    public async Task<TicketDto?> CloseTicketAsync(
        Guid ticketId,
        CloseTicketRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null || ticket.Status == TicketStatus.Closed)
        {
            return null;
        }

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTimeOffset.UtcNow;
        ticket.ChannelCleanupRequested = false;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == ticket.GuildId, cancellationToken);

        if (guild is not null)
        {
            await _logService.CreateLogAsync(new CreateLogRequest
            {
                DiscordGuildId = guild.DiscordGuildId,
                Type = LogEventType.TicketClosed,
                Message = $"Ticket #{ticket.TicketNumber} closed.",
                ActorDiscordUserId = request?.ClosedByDiscordUserId ?? ticket.OwnerDiscordUserId,
                ChannelDiscordId = ticket.ChannelDiscordId,
                ActorDisplayName = request?.ClosedByDisplayName,
                ChannelDisplayName = request?.ChannelDisplayName,
                MetadataJson = LogService.BuildMetadataJson(new { ticket.TicketNumber })
            }, cancellationToken);
        }

        return Map(ticket);
    }

    public async Task<IReadOnlyList<TicketDto>> GetGuildTicketsAsync(
        Guid guildId,
        string ownerDiscordUserId,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(
            guildId,
            ownerDiscordUserId,
            cancellationToken);

        if (!hasAccess)
        {
            return [];
        }

        var tickets = await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.GuildId == guildId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return await EnrichTicketsAsync(guildId, tickets, cancellationToken);
    }

    public async Task<TicketDto?> CloseTicketForGuildAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(
            guildId,
            discordUserId,
            cancellationToken);

        if (!hasAccess)
        {
            return null;
        }

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId && t.GuildId == guildId, cancellationToken);

        if (ticket is null || ticket.Status == TicketStatus.Closed)
        {
            return null;
        }

        ticket.Status = TicketStatus.Closed;
        ticket.ClosedAt = DateTimeOffset.UtcNow;
        ticket.ChannelCleanupRequested = true;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var guild = await _dbContext.Guilds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == guildId, cancellationToken);

        if (guild is not null)
        {
            var actorName = await MemberDisplayNameHelper.ResolveMemberNamesAsync(
                _dbContext,
                guildId,
                [discordUserId],
                cancellationToken);

            await _logService.CreateLogAsync(new CreateLogRequest
            {
                DiscordGuildId = guild.DiscordGuildId,
                Type = LogEventType.TicketClosed,
                Message = $"Ticket #{ticket.TicketNumber} closed from the dashboard.",
                ActorDiscordUserId = discordUserId,
                ActorDisplayName = actorName.GetValueOrDefault(discordUserId),
                ChannelDiscordId = ticket.ChannelDiscordId,
                MetadataJson = LogService.BuildMetadataJson(new { ticket.TicketNumber, source = "dashboard" })
            }, cancellationToken);
        }

        return Map(ticket);
    }

    public async Task<IReadOnlyList<TicketChannelCleanupDto>> GetPendingChannelCleanupsAsync(
        CancellationToken cancellationToken = default)
    {
        var tickets = await _dbContext.Tickets
            .AsNoTracking()
            .Where(t => t.Status == TicketStatus.Closed && t.ChannelCleanupRequested)
            .Select(t => new TicketChannelCleanupDto
            {
                TicketId = t.Id,
                DiscordGuildId = t.Guild.DiscordGuildId,
                ChannelDiscordId = t.ChannelDiscordId,
                TicketNumber = t.TicketNumber,
                OwnerDiscordUserId = t.OwnerDiscordUserId,
                ClosedAt = t.ClosedAt,
                TicketArchiveChannelId = t.Guild.Settings != null
                    ? t.Guild.Settings.TicketArchiveChannelId
                    : null,
                TicketClosedFromDashboardMessage = t.Guild.Settings != null
                    ? t.Guild.Settings.TicketClosedFromDashboardMessage
                    : TicketMessageDefaults.ClosedFromDashboardMessage
            })
            .ToListAsync(cancellationToken);

        if (tickets.Count == 0)
        {
            return tickets;
        }

        var guildIds = tickets
            .Select(t => t.DiscordGuildId)
            .Distinct()
            .ToList();

        var guildIdMap = await _dbContext.Guilds
            .AsNoTracking()
            .Where(g => guildIds.Contains(g.DiscordGuildId))
            .ToDictionaryAsync(g => g.DiscordGuildId, g => g.Id, cancellationToken);

        var enriched = new List<TicketChannelCleanupDto>();

        foreach (var ticket in tickets)
        {
            if (!guildIdMap.TryGetValue(ticket.DiscordGuildId, out var guildId))
            {
                enriched.Add(ticket);
                continue;
            }

            var ownerName = await MemberDisplayNameHelper.ResolveMemberNamesAsync(
                _dbContext,
                guildId,
                [ticket.OwnerDiscordUserId],
                cancellationToken);

            var closedLog = await _dbContext.LogEntries
                .AsNoTracking()
                .Where(le => le.GuildId == guildId
                             && le.Type == LogEventType.TicketClosed
                             && le.ChannelDiscordId == ticket.ChannelDiscordId)
                .OrderByDescending(le => le.CreatedAt)
                .Select(le => new { le.ActorDiscordUserId, le.ActorUsername })
                .FirstOrDefaultAsync(cancellationToken);

            enriched.Add(new TicketChannelCleanupDto
            {
                TicketId = ticket.TicketId,
                DiscordGuildId = ticket.DiscordGuildId,
                ChannelDiscordId = ticket.ChannelDiscordId,
                TicketNumber = ticket.TicketNumber,
                OwnerDiscordUserId = ticket.OwnerDiscordUserId,
                OwnerDisplayName = ownerName.GetValueOrDefault(ticket.OwnerDiscordUserId),
                ClosedByDiscordUserId = closedLog?.ActorDiscordUserId,
                ClosedByDisplayName = closedLog?.ActorUsername ?? "Dashboard",
                ClosedAt = ticket.ClosedAt,
                TicketArchiveChannelId = ticket.TicketArchiveChannelId,
                TicketClosedFromDashboardMessage = ticket.TicketClosedFromDashboardMessage
            });
        }

        return enriched;
    }

    public async Task<bool> AcknowledgeChannelCleanupAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);

        if (ticket is null)
        {
            return false;
        }

        ticket.ChannelCleanupRequested = false;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TicketOutboundMessageDto?> SendTicketMessageAsync(
        Guid guildId,
        Guid ticketId,
        string discordUserId,
        SendTicketMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var hasAccess = await _guildAccessService.CanAccessModerationPagesAsync(
            guildId,
            discordUserId,
            cancellationToken);

        if (!hasAccess)
        {
            return null;
        }

        var content = request.Content.Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Message content is required.");
        }

        if (content.Length > 2000)
        {
            throw new InvalidOperationException("Message content must be 2000 characters or less.");
        }

        var ticket = await _dbContext.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == ticketId && t.GuildId == guildId && t.Status == TicketStatus.Open,
                cancellationToken);

        if (ticket is null)
        {
            return null;
        }

        var senderName = await MemberDisplayNameHelper.ResolveMemberNamesAsync(
            _dbContext,
            guildId,
            [discordUserId],
            cancellationToken);

        var outbound = new TicketOutboundMessage
        {
            TicketId = ticket.Id,
            GuildId = guildId,
            Content = content,
            SenderDiscordUserId = discordUserId,
            SenderDisplayName = senderName.GetValueOrDefault(discordUserId)
        };

        _dbContext.TicketOutboundMessages.Add(outbound);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new TicketOutboundMessageDto
        {
            Id = outbound.Id,
            TicketId = outbound.TicketId,
            Content = outbound.Content,
            SenderDiscordUserId = outbound.SenderDiscordUserId,
            SenderDisplayName = outbound.SenderDisplayName,
            IsDelivered = outbound.IsDelivered,
            CreatedAt = outbound.CreatedAt,
            DeliveredAt = outbound.DeliveredAt
        };
    }

    public async Task<IReadOnlyList<PendingTicketMessageDto>> GetPendingOutboundMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        var pending = await _dbContext.TicketOutboundMessages
            .AsNoTracking()
            .Where(m => !m.IsDelivered)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new PendingTicketMessageDto
            {
                Id = m.Id,
                TicketId = m.TicketId,
                DiscordGuildId = m.Guild.DiscordGuildId,
                ChannelDiscordId = m.Ticket.ChannelDiscordId,
                Content = m.Content,
                SenderDisplayName = m.SenderDisplayName,
                StaffReplyPrefix = m.Guild.Settings != null
                    ? m.Guild.Settings.TicketStaffReplyPrefix
                    : TicketMessageDefaults.StaffReplyPrefix
            })
            .ToListAsync(cancellationToken);

        return pending;
    }

    public async Task<bool> AcknowledgeOutboundMessageAsync(
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        var message = await _dbContext.TicketOutboundMessages
            .FirstOrDefaultAsync(m => m.Id == messageId, cancellationToken);

        if (message is null)
        {
            return false;
        }

        message.IsDelivered = true;
        message.DeliveredAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static TicketDto Map(Ticket ticket) =>
        new()
        {
            Id = ticket.Id,
            GuildId = ticket.GuildId,
            TicketNumber = ticket.TicketNumber,
            OwnerDiscordUserId = ticket.OwnerDiscordUserId,
            ChannelDiscordId = ticket.ChannelDiscordId,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            ClosedAt = ticket.ClosedAt
        };

    private async Task<IReadOnlyList<TicketDto>> EnrichTicketsAsync(
        Guid guildId,
        IReadOnlyList<Ticket> tickets,
        CancellationToken cancellationToken)
    {
        if (tickets.Count == 0)
        {
            return [];
        }

        var memberNames = await MemberDisplayNameHelper.ResolveMemberNamesAsync(
            _dbContext,
            guildId,
            tickets.Select(t => t.OwnerDiscordUserId),
            cancellationToken);

        var channelNames = await MemberDisplayNameHelper.ResolveChannelNamesAsync(
            _dbContext,
            guildId,
            tickets.Select(t => t.ChannelDiscordId),
            cancellationToken);

        return tickets
            .Select(ticket => new TicketDto
            {
                Id = ticket.Id,
                GuildId = ticket.GuildId,
                TicketNumber = ticket.TicketNumber,
                OwnerDiscordUserId = ticket.OwnerDiscordUserId,
                OwnerDisplayName = memberNames.GetValueOrDefault(ticket.OwnerDiscordUserId),
                ChannelDiscordId = ticket.ChannelDiscordId,
                ChannelName = channelNames.GetValueOrDefault(ticket.ChannelDiscordId),
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                ClosedAt = ticket.ClosedAt
            })
            .ToList();
    }
}
