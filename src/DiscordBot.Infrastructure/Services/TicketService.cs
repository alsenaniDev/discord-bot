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

    Task<TicketDto?> CloseTicketAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetGuildTicketsAsync(
        Guid guildId,
        string ownerDiscordUserId,
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _logService.CreateLogAsync(new CreateLogRequest
        {
            DiscordGuildId = request.DiscordGuildId,
            Type = LogEventType.TicketOpened,
            Message = $"Ticket #{ticket.TicketNumber} opened.",
            ActorDiscordUserId = request.OwnerDiscordUserId,
            ChannelDiscordId = request.ChannelDiscordId,
            MetadataJson = LogService.BuildMetadataJson(new { ticket.TicketNumber })
        }, cancellationToken);

        return Map(ticket);
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
                ActorDiscordUserId = ticket.OwnerDiscordUserId,
                ChannelDiscordId = ticket.ChannelDiscordId,
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

        return tickets.Select(Map).ToList();
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
}
